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
            this.SuspendLayout();
            // 
            // btnProceso
            // 
            this.btnProceso.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnProceso.Location = new System.Drawing.Point(60, 90);
            this.btnProceso.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnProceso.Name = "btnProceso";
            this.btnProceso.Size = new System.Drawing.Size(168, 32);
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
            this.label1.Location = new System.Drawing.Point(104, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(181, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "Frecuencia de ejecución";
            // 
            // txtTime
            // 
            this.txtTime.Location = new System.Drawing.Point(25, 33);
            this.txtTime.Name = "txtTime";
            this.txtTime.Size = new System.Drawing.Size(49, 20);
            this.txtTime.TabIndex = 2;
            this.txtTime.Text = "10";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.HotTrack;
            this.ClientSize = new System.Drawing.Size(307, 169);
            this.Controls.Add(this.txtTime);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnProceso);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "Form1";
            this.Text = "Cargar Soportes Ministerio";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnProceso;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtTime;
    }
}

