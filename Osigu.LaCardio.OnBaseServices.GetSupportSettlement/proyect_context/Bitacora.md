# Bitácora de cambios — GetSupportSettlement

## 2026-08-05 — Diseño de envío a RCM + cambio en flujo de archivos

**Autor:** Claude Code (sesión con Henry Martínez)

**Resumen:** Se diseñó completamente el flujo de envío de soportes a la API RCM de Osigu:
- Cambio en `FileSystemSupportDestination`: ahora copia el soporte a `DestinationData.DetinationPath`
  (como hoy) pero además mueve el original a la nueva ruta `RcmStagingPath` para alimentar
  el pipeline de envío a RCM.
- Nuevo `SendSupportToRcmWorker` (BackgroundService independiente) que: clasifica soportes
  (SupportType + invoice number), registra trazabilidad en SQLite local, consulta Servinte
  (Oracle) por información de factura (fecha + monto), construye el mensaje RCM con OAuth2 token,
  envía el archivo vía HTTP multipart, actualiza la trazabilidad y reintenra automáticamente
  hasta un máximo de intentos.
- Nuevas capas: Domain (SupportTraceRecord, ServinteInvoiceInfo), Configuration (RcmApiSettings),
  Application/Ports (ISupportClassifier, ISendSupportToRcm, IServinteInvoiceRepository,
  ISupportTraceStore, IRcmAuthClient, IRcmSupportClient), Infrastructure (todas las
  implementaciones concretas).

**Archivos afectados:**
- Creados: `proyect_context/Bitacora.md` (este archivo), documentación de diseño.
- Modificados: Plan completo en `C:\Users\henmarsa\.claude\plans\hola-en-este-proyecto-wise-perlis.md`

**Motivo:** Garantizar que cada soporte se entregue a RCM (no solo se copie localmente),
con trazabilidad para auditoría y reintentos automáticos ante fallos transitorios.

**Estado:** En implementación — Pasos 1-3 completados. **Bloqueado en NuGet (problema de máquina)**.

**Pasos completados (2026-08-05 sesión 2):**
1. ✅ Bitácora.md creada con formato de entradas
2. ✅ FileSystemSupportSettlement.cs modificado: ahora hace File.Copy a DestinationPath y luego File.Move el original a RcmStagingPath
3. ✅ AppsettingConfiguration.cs: agregado campo `RcmStagingPath`
4. ✅ appsettings.json: agregados sections "RcmApi" y "Servinte" con todas las claves necesarias (placeholders donde corresponde)
5. ✅ RcmApiSettings.cs creado (con BaseUrl, ClientId, ClientSecret, AuthPath, UploadPath, Retries, DelayBetweenRetriesMs)
6. ✅ ServinteSettings.cs creado (con ConnectionString, TraceDatabasePath, RcmSendExecutionFrequency, RcmAgreementCode, SupportFileCodeMapping)
7. ✅ .csproj: agregados paquetes NuGet Oracle.ManagedDataAccess.Core v3.21.120 y Microsoft.Data.Sqlite v7.0.14
8. ✅ nuget.config local creado (solo fuente nuget.org)
9. ✅ global.json creado (pinned SDK 9.0.304)
10. ✅ .vs folder removido (limpiar cache de VS)

**BLOQUEADOR: NuGet NU1301**
El SDK 9.0.304 en esta máquina fuerza una fuente rota: `C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages`
- Carpeta no existe
- No se puede desactivar vía nuget.config (intentado 3 enfoques distintos)
- Requiere reparación de Visual Studio o cambio de SDK

**RESUELTO (2026-08-05 sesión 3):**
La solución fue crear la carpeta fallback con permisos administrativos:
- `mkdir "C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages"` (con RunAs admin)
- Cambio de TargetFramework net7.0 → net8.0 en .csproj (sin soporte activo)
- Actualización de versiones de paquetes a versiones compatibles con net8.0
- Corrección de referencias a AppsettingConfiguration en 4 archivos:
  * FileSystemSupportDestination.cs: acceso a `_appsetting.Configuration.DestinationData` y `.RcmStagingPath`
  * DataQueries.cs: acceso a `_appsetting.Configuration.ConnectionString`
  * SearchSupport.cs: múltiples referencias corregidas
  * GetSupportWorker.cs: acceso a `_appsetting.Configuration.ExecutionFrecuency`
- ✅ Proyecto compila exitosamente (sin errores)

**Pasos completados (2026-08-05 sesión 4):**
4. ✅ Extraer SupportClassifier desde SearchSupport.MappingFiles
   - Creada interfaz ISupportClassifier en Application/Ports
   - Implementada clase SupportClassifier con lógica de clasificación
   - Refactorizado SearchSupport.MappingFiles para usar ISupportClassifier
   - Commit: refactor: extract SupportClassifier from SearchSupport
5. ✅ Implementados Domain y Ports para RCM integration
   - SupportTraceRecord (Domain): modelo de trazabilidad
   - ISupportTraceStore (Ports): persistencia de trazabilidad
   - ISendSupportToRcm (Ports): orquestación de envío
   - IServinteInvoiceRepository (Ports): acceso a Oracle
   - IRcmAuthClient (Ports): autenticación OAuth2
   - IRcmSupportClient (Ports): cliente HTTP para upload
   - DTOs: RcmAuthToken, ServinteInvoiceInfo, RcmUploadRequest, RcmUploadResponse
   - Commit: feat: add domain models and port interfaces for RCM integration

**BLOQUEADOR RESUELTO (2026-08-06):**
Se descubrió que la tabla `SupportTrace` (SQLite local) nunca recibía inserts porque `ISupportTraceStore.AddTraceAsync` nunca era invocado en ningún punto del código. Esto causaba que `SendSupportToRcmWorker` siempre encontrara la cola vacía (`GetPendingTracesAsync()` retornaba lista vacía) y se quedara suspendido.

Solución: Inyectar `ISupportTraceStore` en `FileSystemSupportDestination` y registrar un `SupportTraceRecord` con estado "Pending" inmediatamente después de mover cada archivo a `RcmStagingPath`. Esto conecta al fin el productor (movimiento de archivos) con el consumidor (procesamiento de cola RCM).

Cambio:
- `FileSystemSupportDestination.cs`: inyectar `ISupportTraceStore`, después de `File.Move(...)` (línea ~49) llamar a `_traceStore.AddTraceAsync(...)` con los datos: `InvoiceNumber`, `SupportType`, `FilePath` (ruta final en staging), `Status = "Pending"`, `AttemptCount = 0`.
- No se requirieron cambios en otros archivos (el DI ya tenía todo registrado).
- Commit: cda6d3f "feat: register support trace when moving files to RcmStagingPath"

**OPTIMIZACIÓN: Agrupación de consultas a Servinte (2026-08-06):**
Se descubrió que al procesar soportes pendientes, se consultaba Oracle (Servinte) una vez **por cada trace individual**, incluso si varios comparten el mismo `InvoiceNumber` (ej. una factura con soportes FACTURA, CUV, CUV-TEXT generaba 3 traces pendientes = 3 consultas Oracle idénticas a la misma factura).

Solución implementada en `Application/SendSupportToRcm.cs`:
- Refactorizar `SendPendingSupportAsync` en dos versiones: pública (obtiene invoiceInfo) que delega a privada (recibe invoiceInfo como parámetro).
- Reescribir `SendBatchAsync` para:
  * Agrupar traces por `InvoiceNumber` usando `GroupBy(t => t.InvoiceNumber)`
  * Consultar Servinte una sola vez por grupo (por factura única)
  * Reutilizar `ServinteInvoiceInfo` para todos los traces de esa factura
  * Mantener delay por-trace hacia RCM API (preserva throttling actual)
- Resultado: 1 consulta Oracle por factura (N soportes), no N consultas.
- Commit: 51a40bb "perf: group support traces by invoice number to reduce redundant Oracle queries"

**Próximos pasos:**
6. Implementar SqliteSupportTraceStore (Infrastructure)
7. Implementar OracleServinteInvoiceRepository (Infrastructure)
8. Implementar RcmAuthClient con token cacheado (Infrastructure)
9. Implementar RcmSupportClient con reintentos (Infrastructure)
10. Implementar SendSupportToRcm (Application/orquestación)
11. Implementar SendSupportToRcmWorker (Workers)
12. Registrar servicios en Program.cs (DI)
13. Pruebas end-to-end

**Notas técnicas:**
- Servinte es Oracle (no SQL Server como OnBase): host 192.168.61.130:1521, servicio PRDDAT, usuario SERVINTE. Tablas: `famov` (encabezado), `famovdet` (detalle).
- RCM endpoint sandbox: `https://sandbox.osigu.com`, auth en `/v1/oauth/token`, upload en `/rcm/v1/support-files/upload`.
- Principios de código: SOLID/KISS, legible "como literatura", nombres autoexplicativos, funciones pequeñas de un solo nivel de abstracción, manejo de errores en el borde (Infrastructure).
- TODOs de negocio pendientes de confirmar (marcados en el código): `support_file_code` por SupportType, `agreement_code` (fijo vs dinámico), `origin_event_id`, `process_id`, `document_type`, `invoice_electronic_code`.

**Soluciones sugeridas para NuGet NU1301:**
1. Ejecutar reparación de Visual Studio Community 2022 (VS Installer → Repair)
2. O cambiar .csproj TargetFramework a net8.0 o net9.0 (más nuevos, sin este bug)
3. O crear la carpeta vacía `C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages` y ejecutar una restauración offline desde nuget.org
