using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Scripts_Varios
{
    public class BuscarXmlMederi
    {
        public void BuscarXml()
        {
            string rutaArchivo = @"C:\Users\henmarsa\Documents\FacturasMederiXML.txt";
            string rutaDirectorio = @"\\192.168.5.22\Rips_json\Historico_CUV";
            string rutaSalida = @"\\192.168.5.22\Rips_json\Lista archivos xml";
            DirectoryInfo directorioActual;
            List<string> listaArchivosDirectorio = new List<string>();
            List<string> listaArchivos = new List<string>();
            List<DirectoryInfo> directorios = new List<DirectoryInfo>();
            List<DirectoryInfo> directoriosBusqueda = new List<DirectoryInfo>();
            List<DatosArchivosBusqueda> listaDatosArchivosBusqueda = new List<DatosArchivosBusqueda>();

            using (StreamReader sr = new StreamReader(rutaArchivo))
            {
                string linea;
                // Lee línea por línea hasta que sea null (fin del archivo)
                while ((linea = sr.ReadLine()) != null)
                {
                    listaArchivos.Add(linea); // Añade la línea a la lista
                }
            }


            if (Directory.Exists(rutaDirectorio))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(rutaDirectorio);

                directorios = directoryInfo.GetDirectories().ToList();
            }


            foreach (string file in listaArchivos)
            {
                DatosArchivosBusqueda datosArchivosBusqueda = new DatosArchivosBusqueda();
                string archivo = string.Empty;
                datosArchivosBusqueda.DirectorioBusqueda = (directorios.Where(x => x.Name.Contains(file)).FirstOrDefault());
                datosArchivosBusqueda.NombreArchivo = file;
                listaDatosArchivosBusqueda.Add(datosArchivosBusqueda);

            }

            foreach (DatosArchivosBusqueda datosArchivo in listaDatosArchivosBusqueda)
            {
                if (datosArchivo.DirectorioBusqueda is not null)
                {
                    var d = datosArchivo.DirectorioBusqueda.GetFiles().Where(x => x.Extension.ToLower() == ".xml").ToList();
                    //var f = directorioActual.GetFiles();
                    datosArchivo.RutaArchivo = d.FirstOrDefault().FullName;
                    datosArchivo.existe = true;
                }
                else
                {
                    datosArchivo.existe = false;
                }
            }

            foreach (DatosArchivosBusqueda datosArchivos in listaDatosArchivosBusqueda.Where(x=> x.existe=true))
            {
                try { 
                File.Copy(datosArchivos.RutaArchivo, $@"{rutaSalida}\{datosArchivos.NombreArchivo}.xml");
                }

                catch (Exception e)
                {
                    datosArchivos.existe = false;
                }
            }

            using (StreamWriter writer = new StreamWriter(@"C:\Users\henmarsa\Documents\FacturasMederiXMLResultado.txt", append: true))
            {
                foreach(DatosArchivosBusqueda datosArchivos in listaDatosArchivosBusqueda)
                {
                    writer.WriteLine($@"{datosArchivos.NombreArchivo}; {datosArchivos.existe}");
                }
                
            }
        }

        public class DatosArchivosBusqueda
        {
            public string RutaArchivo { get; set; }
            public string NombreArchivo { get; set; }
            public DirectoryInfo DirectorioBusqueda { get; set; }
            public bool existe { get; set; }


        }
    }
}

