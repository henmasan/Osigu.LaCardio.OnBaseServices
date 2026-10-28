using Newtonsoft.Json;
using ProcesadorSoportesMinisterio.Model;
using ProcesadorSoportesMinisterio.Rcm;
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
        private Timer _rcmTimer;
        private string Estado;
        private AppsettingConfiguration _appSettings;
        private ISupportTraceStore _traceStore;
        private INotFoundSupportStore _notFoundStore;
        private ISendSupportToRcm _sendSupportToRcm;
        private bool _rcmProcessing = false;

        public Form1()
        {
            InitializeComponent();

            _timer = new Timer();
            _timer.Interval = Convert.ToInt32(txtTime.Text) * 60000;
            _timer.Tick += OnTimedEvent;

            _rcmTimer = new Timer();
            _rcmTimer.Tick += OnRcmTimerTick;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(Estado))
            {
                Estado = "Iniciado";
                txtTime.ReadOnly = true;
                btnProceso.Text = "Detener";
                //InitializeRcmServices();
                _timer.Start();
            }
            else if (Estado == "Iniciado")
            {
                Estado = "Detenido";
                txtTime.ReadOnly = false;
                btnProceso.Text = "Iniciar";
                _timer.Stop();
                _rcmTimer.Stop();
            }

            else if (Estado == "Detenido")
            {
                Estado = "Iniciado";
                txtTime.ReadOnly = true;
                btnProceso.Text = "Detener";
                _timer.Start();
                if (_appSettings?.Configuration?.SendSupportToRcmEnabled == true)
                {
                    _rcmTimer.Start();
                }
            }
            ProcessSupport();
        }
        private void OnTimedEvent(object sender, EventArgs e)
        {
            ProcessSupport();
        }

        private void InitializeRcmServices()
        {
            try
            {
                if (_appSettings == null)
                {
                    string path = GetAppsettingsPath();
                    _appSettings = LoadAppSettings(path);
                }

                if (_appSettings?.Configuration?.SendSupportToRcmEnabled == true && _appSettings?.RcmApi != null && _appSettings?.Servinte != null)
                {
                    string rcmStagingPath = _appSettings.Configuration?.RcmStagingPath;
                    if (string.IsNullOrEmpty(rcmStagingPath))
                    {
                        throw new Exception("RcmStagingPath is not configured");
                    }

                    _traceStore = new SqliteSupportTraceStore(Path.Combine(rcmStagingPath, "support_trace.db"));

                    IServinteInvoiceRepository servinteRepository;
                    if (_appSettings.Servinte?.DatabaseOrigin?.Equals("Informix", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        servinteRepository = new InformixServinteInvoiceRepository(_appSettings);
                    }
                    else
                    {
                        servinteRepository = new OracleServinteInvoiceRepository(_appSettings);
                    }

                    var rcmAuthClient = new RcmAuthClient(_appSettings.RcmApi);
                    var rcmSupportClient = new RcmSupportClient(_appSettings.RcmApi);

                    _sendSupportToRcm = new SendSupportToRcm(
                        _traceStore,
                        servinteRepository,
                        rcmAuthClient,
                        rcmSupportClient,
                        _appSettings.RcmApi,
                        _appSettings.Servinte,
                        rcmStagingPath
                    );

                    int rcmFrequency = _appSettings.Servinte?.RcmSendExecutionFrequency ?? 60000;
                    if (rcmFrequency <= 0) rcmFrequency = 60000;
                    _rcmTimer.Interval = rcmFrequency;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing RCM services: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnRcmTimerTick(object sender, EventArgs e)
        {
            SendSupportToRcm();
        }

        private string GetAppsettingsPath()
        {
            return string.Format($"{System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName)}\\appsettings.json");
        }

        private AppsettingConfiguration LoadAppSettings(string path)
        {
            StringBuilder contentFile = new StringBuilder();
            using (StreamReader sr = new StreamReader(path))
            {
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    contentFile.AppendLine(line);
                }
            }

            string docJson = contentFile.ToString();
            return JsonConvert.DeserializeObject<AppsettingConfiguration>(docJson);
        }

        public void SendSupportToRcm()
        {
            if (_rcmProcessing || _sendSupportToRcm == null)
            {
                return;
            }

            _rcmProcessing = true;
            try
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _sendSupportToRcm.ProcessQueueAsync();
                    }
                    catch (Exception ex)
                    {
                        // Log error
                    }
                    finally
                    {
                        _rcmProcessing = false;
                    }
                });
            }
            catch (Exception ex)
            {
                _rcmProcessing = false;

            }
        }

        public void ProcessSupport()
        {
            try
            {
                if (_appSettings == null)
                {
                    string path = GetAppsettingsPath();
                    _appSettings = LoadAppSettings(path);
                }

                if (_notFoundStore == null)
                {
                    _notFoundStore = new SqliteNotFoundSupportStore(Path.Combine(_appSettings.Configuration.NotFoundPath, "not_found.db"));
                }

                SearchSupport searchSupport = new SearchSupport(_appSettings.Configuration, _notFoundStore, _traceStore);
                searchSupport.FindSupportInFolder();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnEnviarRCM_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Estado))
            {
                Estado = "Iniciado";
                txtTime.ReadOnly = true;
                btnEnviarRCM.Text = "Detener";
                InitializeRcmServices();
                _rcmTimer.Start();

            }
            else if (Estado == "Iniciado")
            {
                Estado = "Detenido";
                txtTime.ReadOnly = false;
                btnEnviarRCM.Text = "Iniciar";
                _timer.Stop();
                _rcmTimer.Stop();
            }

            else if (Estado == "Detenido")
            {
                Estado = "Iniciado";
                txtTime.ReadOnly = true;
                btnEnviarRCM.Text = "Detener";
                _timer.Start();
                if (_appSettings?.Configuration?.SendSupportToRcmEnabled == true)
                {
                    _rcmTimer.Start();
                }
            }
            SendSupportToRcm();
        }

        //private void buttonStop_Click(object sender, EventArgs e)
        //{
        //    _timer.Stop(); // Detener el temporizador
        //    labelStatus.Text = "Temporizador detenido.";
        //}
    }
}
