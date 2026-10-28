using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts_Varios
{
    public class BuscarSoportesCuvHistoricos
    {
        public void BuscarSoportes()
        {
            string rutaArchivo = @"C:\Users\henmarsa\Documents\OnBase\Mederi\Soportes\Soportes ministerio a procesar\SoportesPendientes.txt";
            string rutaDirectorio = @"\\192.168.5.22\Rips_json\Historico_CUV";
            string rutaSalida = @"C:\Users\henmarsa\Documents\OnBase\Mederi\Soportes\Soportes ministerio a procesar\Pendientes";
            List<string> listaArchivos = new List<string>();

            List<BusquedaDirectorios> listaBusquedaDirectorios = new List<BusquedaDirectorios>();
            List<DatosArchivosBusqueda> listaDatosArchivosBusqueda = new List<DatosArchivosBusqueda>();

            using (StreamReader sr = new StreamReader(rutaArchivo))
            {
                string linea;
                while ((linea = sr.ReadLine()) != null)
                {
                    listaArchivos.Add(linea); // Añade la línea a la lista
                }
            }




            foreach (string archivo in listaArchivos)
            {
                string ruta = Path.Combine(rutaDirectorio, archivo);
                BusquedaDirectorios busqueda = new BusquedaDirectorios();
                busqueda.RutaDirectorio = ruta;

                if (Directory.Exists(ruta))
                {
                    busqueda.Existe = true;
                }

                else
                {
                    busqueda.Existe = false;
                }

                listaBusquedaDirectorios.Add(busqueda);
            }


            using (StreamWriter writer = new StreamWriter(@"C:\Users\henmarsa\Documents\OnBase\Mederi\Soportes\Soportes ministerio a procesar\BusquedaDirectoriosResultado.txt", append: true))
            {
                foreach (BusquedaDirectorios busqueda in listaBusquedaDirectorios)
                {
                    writer.WriteLine($@"{busqueda.RutaDirectorio}; {busqueda.Existe}");
                }

            }

            if (listaBusquedaDirectorios.Any(b => b.Existe))
            {
                const string nit = "900210981";
                foreach (BusquedaDirectorios busqueda in listaBusquedaDirectorios.Where(b => b.Existe))
                {
                    string ruta = busqueda.RutaDirectorio;
                    DirectoryInfo directorio = new DirectoryInfo(ruta);
                    FileInfo[] archivos = directorio.GetFiles();
                    foreach (FileInfo archivo in archivos)
                    {
                        var partesArchivo = archivo.Name.Split('_');
                        string tipo = partesArchivo[0];
                        var partesFactura = partesArchivo[1].Split('.');
                        string factura = partesFactura[0];
                        string extension = partesFactura[1];


                        string nuevoNombreArchivo = $"{nit}_{factura}_{tipo.ToLower()}.{extension}";


                        DatosArchivosBusqueda datosArchivo = new DatosArchivosBusqueda();
                        datosArchivo.RutaArchivo = archivo.FullName;
                        datosArchivo.NombreArchivo = archivo.Name;
                        datosArchivo.NuevoNombreArchivo = nuevoNombreArchivo;
                        datosArchivo.DirectorioBusqueda = directorio;
                        datosArchivo.existe = true;
                        listaDatosArchivosBusqueda.Add(datosArchivo);
                        string nuevaRutaSalida = Path.Combine(rutaSalida, directorio.Name);
                        if(!Directory.Exists(nuevaRutaSalida))
                        {
                            Directory.CreateDirectory(nuevaRutaSalida);
                        }

                        try
                        {
                            File.Copy(archivo.FullName, Path.Combine(nuevaRutaSalida, nuevoNombreArchivo));
                            datosArchivo.copiado = true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al copiar el archivo {archivo.Name}: {ex.Message}");
                            datosArchivo.copiado = false;
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("No se encontraron directorios existentes.");
            }

            foreach (DatosArchivosBusqueda datosArchivo in listaDatosArchivosBusqueda)
            {
                using (StreamWriter writer = new StreamWriter(@"C:\Users\henmarsa\Documents\OnBase\Mederi\Soportes\Soportes ministerio a procesar\ResultadoBusquedaArchivos.txt", append: true))
                {
                    writer.WriteLine($@"{datosArchivo.RutaArchivo}; {datosArchivo.NombreArchivo}; {datosArchivo.NuevoNombreArchivo}; {datosArchivo.DirectorioBusqueda.FullName}; {datosArchivo.existe}; {datosArchivo.copiado}");
                }
            }
        }

        public class DatosArchivosBusqueda
        {
            public string RutaArchivo { get; set; }
            public string NombreArchivo { get; set; }
            public string NuevoNombreArchivo { get; set; }
            public DirectoryInfo DirectorioBusqueda { get; set; }
            public bool existe { get; set; }
            public bool copiado { get; set; }


        }

        public class BusquedaDirectorios
        {
            public string RutaDirectorio { get; set; }
            public bool Existe { get; set; }
        }

    }
}
