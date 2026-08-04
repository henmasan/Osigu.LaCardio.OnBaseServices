using Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Osigu.LaCardio.OnBaseServices.GetSupportSettlement.Application.Ports
{
    /// <summary>
    /// Puerto de salida: representa el destino donde un soporte procesado debe entregarse.
    /// Hoy la única implementación es FileSystemSupportDestination (mueve el archivo físico
    /// y anexa su índice a Support{Invoice}.txt).
    /// Punto de extensión futuro: agregar ApiSupportDestination (envío de copia a API externa)
    /// y/o un CompositeSupportDestination / decorator que combine ambas entregas, agregue
    /// tracking de estado, reintentos e idempotencia — sin tocar SearchSupport.
    /// </summary>
    public interface ISupportDestination
    {
        Task DeliverAsync(List<Support> supports, string invoiceNumber);
    }
}
