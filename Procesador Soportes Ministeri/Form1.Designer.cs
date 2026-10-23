namespace ProcesadorSoportesMinisterio
{
    partial class Form1
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnProceso = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtTime = new System.Windows.Forms.TextBox();
            this.btnEnviarRCM = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnProceso
            // 
            this.btnProceso.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnProceso.Location = new System.Drawing.Point(367, 109);
            this.btnProceso.Name = "btnProceso";
            this.btnProceso.Size = new System.Drawing.Size(252, 49);
            this.btnProceso.TabIndex = 0;
            this.btnProceso.Text = "Iniciar";
            this.btnProceso.UseVisualStyleBackColor = true;
            this.btnProceso.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.label1.Location = new System.Drawing.Point(156, 51);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(277, 29);
            this.label1.TabIndex = 1;
            this.label1.Text = "Frecuencia de ejecución";
            // 
            // txtTime
            // 
            this.txtTime.Location = new System.Drawing.Point(38, 51);
            this.txtTime.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtTime.Name = "txtTime";
            this.txtTime.Size = new System.Drawing.Size(72, 26);
            this.txtTime.TabIndex = 2;
            this.txtTime.Text = "10";
            // 
            // btnEnviarRCM
            // 
            this.btnEnviarRCM.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnEnviarRCM.Location = new System.Drawing.Point(367, 187);
            this.btnEnviarRCM.Name = "btnEnviarRCM";
            this.btnEnviarRCM.Size = new System.Drawing.Size(252, 49);
            this.btnEnviarRCM.TabIndex = 3;
            this.btnEnviarRCM.Text = "Iniciar";
            this.btnEnviarRCM.UseVisualStyleBackColor = true;
            this.btnEnviarRCM.Click += new System.EventHandler(this.btnEnviarRCM_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.Window;
            this.label2.Location = new System.Drawing.Point(70, 198);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(244, 29);
            this.label2.TabIndex = 4;
            this.label2.Text = "Enviar Soportes RCM";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.Window;
            this.label3.Location = new System.Drawing.Point(70, 120);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(214, 29);
            this.label3.TabIndex = 5;
            this.label3.Text = "Procesar Soportes";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.HotTrack;
            this.ClientSize = new System.Drawing.Size(665, 303);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnEnviarRCM);
            this.Controls.Add(this.txtTime);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnProceso);
            this.Name = "Form1";
            this.Text = "Cargar Soportes Ministerio";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnProceso;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtTime;
        private System.Windows.Forms.Button btnEnviarRCM;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}

