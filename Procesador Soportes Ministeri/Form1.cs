using Newtonsoft.Json;
using ProcesadorSoportesMinisterio.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProcesadorSoportesMinisterio
{
    public partial class Form1 : Form
    {
        private Timer _timer;
        private string Estado;
        public Form1()
        {
            InitializeComponent();

            _timer = new Timer();
            _timer.Interval = Convert.ToInt32(txtTime.Text) * 60000 ; 
            _timer.Tick += OnTimedEvent; 
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            if (string.IsNullOrEmpty(Estado))
            {
                Estado = "Iniciado";
                txtTime.ReadOnly=true;
                btnProceso.Text = "Detener";
                _timer.Start();
            }
            else if(Estado == "Iniciado")
            {
                Estado = "Detenido";
                txtTime.ReadOnly = false;
                btnProceso.Text = "Iniciar";
                _timer.Stop();
            }

            else if (Estado == "Detenido")
            {
                Estado = "Iniciado";
                txtTime.ReadOnly = true;
                btnProceso.Text = "Detener";
                _timer.Start();
            }
            ProcessSupport();
        }
        private void OnTimedEvent(object sender, EventArgs e)
        {
            ProcessSupport();
        }

        public void ProcessSupport()
        {
            try
            {

                string path = string.Format($"{System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName)}\\appsettings.json");

                StringBuilder contentFile = new StringBuilder();
                using (StreamReader sr = new StreamReader(path))
                {
                    string line = string.Empty;
                    while ((line = sr.ReadLine()) != null)
                    {
                        contentFile.AppendLine(line);
                    }

                }

                Configuration configuration = new Configuration();

                string docJson = contentFile.ToString();

                var appSetting = JsonConvert.DeserializeObject<AppsettingConfiguration>(docJson);


                SearchSupport searchSupport = new SearchSupport(appSetting.Configuration);
                searchSupport.FindSupportInFolder();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //private void buttonStop_Click(object sender, EventArgs e)
        //{
        //    _timer.Stop(); // Detener el temporizador
        //    labelStatus.Text = "Temporizador detenido.";
        //}
    }
}
