 /* Sistema Supervisório
 * Plataforma Didática para implementação e avaliação de controladores discretos
 * Trabalho de Conclusão de Curso de Engenharia de Controle Automação
 * Ifes - Campus Serra
 * Autor: Rafael Elias de Sousa
 * Orientador: Prof. Dr. Saul Munareto*/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;

namespace UniControl
{

    public partial class Form1 : Form
    {
        TextBox MemBufferRec= new TextBox();
        bool pause_graph = false, grava = false, espera_dadoscont = false, rec_dadoscont = false;
        byte status_cfginfb = 100;
        byte[] dados_cont = new byte[27];
        double PV = 0, MV = 0, SP = 0, TRec = 0, TRecInc = 0;
        public int cntselt = 0;
        Config_Control frm;
        public Form1()
        {
            InitializeComponent();
            serialPort1.NewLine = "\n";
            frm = new Config_Control(serialPort1);
            MemBufferRec.Multiline = true;
        }


        private void button2_Click(object sender, EventArgs e)
        {
           
            int t_aq = int.Parse(comboBox2.Text);
            if (t_aq > 0) { 
            byte[] t_aqv = { 0, 0, 0, 0 };
            t_aqv[0] = 65;
            t_aqv[1] = (byte)(t_aq);
            t_aqv[2] = (byte)(t_aq >> 8);
            t_aqv[3] = (byte)(t_aqv[1] ^ t_aqv[2]);
            try { serialPort1.Write(t_aqv, 0, 4); }
            catch { MessageBox.Show("Erro ao enviar novo período de aquisição", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            byte[] temp = { 72, 0, 0};
            if  (feedbackbtn.Text == "Fechar Malha")
            temp[1] = 1;
            else temp[1] = 0;
            temp[2] = temp[1];
            serialPort1.Write(temp, 0, 3);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            MemBufferRec.AppendText(Convert.ToString(TRec) + ";" + Convert.ToString(PV) + ";" + Convert.ToString(MV) + ";" + Convert.ToString(SP) + Environment.NewLine);
            TRec += TRecInc;

        }


        private void ConfigInFb(byte cfg)
        {
            if ((cfg==80) | (cfg == 81))
            {
                status_cfginfb = 80;
                pictureBox1.Image = Properties.Resources.Malha_Aberta;
                pictureBox1.Refresh();
                feedbackbtn.Text = "Fechar Malha";
                enviarmv.Visible = true;
                enviarsp.Visible = false;
                textBox2.Enabled = true;
                textBox3.Enabled = false;
                label5.Text = "V";
                label7.Text = "V";
                label14.Text = "V";
                label16.Text = "V";
                if (cfg==81)
                {
                    status_cfginfb = 81;
                    label5.Text = "mA";
                    label7.Text = "mA";
                    label14.Text = "mA";
                    label16.Text = "mA";
                }
            }
            if ((cfg == 82) | (cfg == 83))
            {
                status_cfginfb = 82;
                pictureBox1.Image = Properties.Resources.Malha_Fechada;
                pictureBox1.Refresh();
                feedbackbtn.Text = "Abrir Malha";
                enviarmv.Visible = false;
                enviarsp.Visible = true;
                textBox2.Enabled = false;
                textBox3.Enabled = true;
                label5.Text = "V";
                label7.Text = "V";
                label14.Text = "V";
                label16.Text = "V";
                if (cfg == 83)
                {
                    status_cfginfb = 83;
                    label5.Text = "mA";
                    label7.Text = "mA";
                    label14.Text = "mA";
                    label16.Text = "mA";
                }
            }

        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if ((serialPort1.BytesToRead >= 27) && espera_dadoscont)
            {
                serialPort1.Read(dados_cont, 0, 27);
                this.BeginInvoke((Action)(() => AtualizaControlador()));
                serialPort1.DiscardInBuffer();
                serialPort1.ReceivedBytesThreshold = 6;
                espera_dadoscont = false;
            }
            if ((serialPort1.BytesToRead > 6) && !espera_dadoscont) 
            serialPort1.DiscardInBuffer();
            if ((serialPort1.BytesToRead == 6) && !espera_dadoscont)
            {
                byte[] dados_bytes = new byte[6];
                serialPort1.Read(dados_bytes, 0, 6);
                if ((dados_bytes[0] == 84) && ((dados_bytes[1] ^ dados_bytes[2] ^ dados_bytes[3] ^ dados_bytes[4] ^ dados_bytes[5]) == 0))
                {
                    espera_dadoscont = true;
                    serialPort1.ReceivedBytesThreshold = 27;
                }
                if ((dados_bytes[0] >= 80) & (dados_bytes[0] <= 83))
                {
                    this.BeginInvoke((Action)(() =>
                    {
                    if ((dados_bytes[1] ^ dados_bytes[2] ^ dados_bytes[3] ^ dados_bytes[4] ^ dados_bytes[5]) == 0)
                        {
                            PV = ((double)(BitConverter.ToInt16(dados_bytes,1)) / 100);
                            MV = ((double)(BitConverter.ToInt16(dados_bytes, 3)) / 100);
                            if (dados_bytes[0] != status_cfginfb )
                            ConfigInFb(dados_bytes[0]);
                            label12.Text = PV.ToString("0.00");
                            textBox1.Text = label12.Text;
                            label10.Text = MV.ToString("0.00");
                            if (status_cfginfb <= 81)
                              SP = PV;
                            label9.Text = SP.ToString("0.00");
                            if (grafico.Series[0].Points.Count > 1000)
                            {
                                grafico.Series[0].Points.RemoveAt(0);
                                grafico.Series[1].Points.RemoveAt(0);
                                grafico.Series[2].Points.RemoveAt(0);
                            }
                            if (!pause_graph)
                            {
                                grafico.Series[0].Points.Add(SP);
                                grafico.Series[1].Points.Add(MV);
                                grafico.Series[2].Points.Add(PV);

                            }
                            if (!rec_dadoscont)
                            { 
                                byte[] recdadoscont = { 76, 1, 1 };
                                serialPort1.Write(recdadoscont, 0, 3);
                            }
                            grafico.ResetAutoValues();
                        }

                    }));
                } else
                {
                    serialPort1.DiscardInBuffer();
                 }
                
            }
            
        }

     
        private void button3_Click(object sender, EventArgs e)
        {
           
            int SPtemp = (int)(Math.Round(Double.Parse(textBox3.Text), 2) * 100);
            if (SPtemp > 2000)
            {
                textBox3.Text = "20";
                SPtemp = 2000;
            }
            if (SPtemp < 0)
            {
                textBox3.Text = "0";
                SPtemp = 0;
            }
            SP = ((double)SPtemp) / 100;
            byte[] temp = { 67, 0, 0, 0 };
            temp[1] = (byte)(SPtemp);
            temp[2] = (byte)(SPtemp >> 8);
            temp[3] = (byte)(temp[1] ^ temp[2]);
            serialPort1.Write(temp,0,4);
            
        }

        private void button9_Click(object sender, EventArgs e)
        {
     
            pause_graph = !pause_graph;
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!grava) {
                try
                {
                    timer1.Interval = int.Parse(comboBox2.Text);
                    timer1.Enabled = true;
                    grava = true;
                    TRec = 0;
                    TRecInc = ((double)(int.Parse(comboBox2.Text))) / 1000;
                    button5.Text = "Parar";
                    button5.BackColor = Color.Red;
                    comboBox2.Enabled = false;
                    BarraStatus.Text = "Gravando arquivo CSV... Período de aquisiçao: " + comboBox2.Text + " ms. Pare para salvar!";

                }
                catch
                {
                    MessageBox.Show("Não foi possível iniciar a gravação.", "Erro", MessageBoxButtons.OK);
                }
    
            } else
            {
                timer1.Enabled = false;
                grava = false;
                button5.Text = "Gravar";
                button5.BackColor = Color.FromArgb(224, 224, 224);
                if (!serialPort1.IsOpen)
                BarraStatus.Text = "Conectado a porta: " + serialPort1.PortName;
                comboBox2.Enabled = true;
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    File.WriteAllText(saveFileDialog1.FileName, MemBufferRec.Text);
                else
                {
                    File.WriteAllText("log.csv", MemBufferRec.Text);
                    MessageBox.Show("Dados foram salvos no arquivo log.csv.", "Arquivo CSV",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                }
                
                MemBufferRec.Clear();

            }

        }

  

        private void enviarmv_Click(object sender, EventArgs e)
        {
            int mvtemp = (int)(Math.Round(Double.Parse(textBox2.Text), 2) * 100);
            if (mvtemp > 1000)
            {
                textBox2.Text = "10";
                mvtemp = 1000;
            }
            if (mvtemp < 0)
            {
                textBox2.Text = "0";
                mvtemp = 0;
            }
            byte[] temp = { 66, 0, 0, 0 };
            temp[1] = (byte)(mvtemp);
            temp[2] = (byte)(mvtemp >> 8);
            temp[3] = (byte)(temp[1] ^ temp[2]);
            serialPort1.Write(temp, 0, 4);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
        }

        private void comboBox1_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            if ((!serialPort1.IsOpen) & (button1.Text == "Conectar") & (comboBox1.Text != ""))
            {
                try
                {
                    serialPort1.Open();
                    button1.Text = "Desconectar";
                    comboBox1.Enabled = false;
                    BarraStatus.Text = "Conectado a porta: " + serialPort1.PortName;
                    int t_aq = int.Parse(comboBox2.Text);
                    rec_dadoscont = false;
                    if (t_aq > 0)
                    {
                        byte[] t_aqv = { 65, 0, 0, 0}; 
                        t_aqv[1] = (byte)(t_aq);
                        t_aqv[2] = (byte)(t_aq >> 8);
                        t_aqv[3] = (byte)(t_aqv[1] ^ t_aqv[2]);
                        try { serialPort1.Write(t_aqv, 0, 4); }
                        catch { MessageBox.Show("Erro ao enviar novo período de aquisição", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); }
                    }
                }
                catch
                {
                    MessageBox.Show("Não foi possível conectar. A porta pode estar ocupada!", "Erro", MessageBoxButtons.OK);
                }
            }
            else
            {
                try
                {
                    serialPort1.Close();
                    button1.Text = "Conectar";
                    comboBox1.Enabled = true;
                    BarraStatus.Text = "Aguardando conexão.";
                }
                catch
                {
                    return;
                }

            }


        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            serialPort1.PortName = comboBox1.Text;
        }

        private void habilitarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Os dados do controlador, setpoint e status da malha serão gravados na Memória Não Volátil. \r\nOs dados contidos na memória são recuperados a cada inicialização do hardware se essa função for habilitada.\r\nAntes de gravar, certifique-se do correto funcionamento do controlador e ajuste do Setpoint.\r\nDeseja habilitar o modo autônomo com o controlador em funcionamento atual?", "Memória EEPROM",
           MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                if (status_cfginfb < 82)
                {
                    if (MessageBox.Show("Recomenda-se fechar a malha antes de gravar a memória para que o controlador possa funcionar em modo autônomo.\r\nDeseja fechar a malha antes de gravar? ", "Fechar malha?",
                   MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        byte[] temp = { 72, 1, 1 };
                        serialPort1.Write(temp, 0, 3);

                    }

                }
                byte[] recmemnvol = { 74, 1, 1 };
                try { serialPort1.Write(recmemnvol, 0, 3);
                    MessageBox.Show("Modo Autônomo Habilitado.", "Modo Autônomo Habilitado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { MessageBox.Show("Erro ao enviar comando para gravação da memória.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); return; }
               
            }
        }

        private void desabilitarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte[] recmemnvol = { 75, 1, 1 };
            try { serialPort1.Write(recmemnvol, 0, 3);
                MessageBox.Show("Modo Autônomo Desabilitado.", "Modo Autônomo Desabilitado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch { MessageBox.Show("Erro ao enviar comando para gravação da memória.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); return; }
         
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            BarraStatus.Text = "Arquivo CSV Salvo em: " + saveFileDialog1.FileName;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)  
                enviarmv.PerformClick();
          
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
                enviarsp.PerformClick();
        }

        private void sobreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Plataforma Didática para Implementação e Avaliação de Controladores Discretos\r\nTCC de Engenharia de Controle e Automação\r\nIfes - Campus Serra\r\nAutor: Rafael Elias de Sousa\r\nOrientador: Prof. Dr. Saul Munareto", "Sobre", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void SalvarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog2.ShowDialog() == DialogResult.OK)
                grafico.SaveImage(saveFileDialog2.FileName, ChartImageFormat.Png);
        }

        private void oQueÉToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("O modo autônomo permite usar o hardware de forma independente do computador. Após configuração do controlador e habilitação do modo, pode-se desplugar o hardware do computador e usá-lo na planta a ser controlada, com setpoint fixo e pré-ajustado, alimentando o hardware com uma fonte independente 5V (carregador de celular por exemplo).", "Modo Autônomo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ConfigurarToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            frm.ShowDialog();
        }

        private void pausarGráficoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pausarGráficoToolStripMenuItem.Checked)
                pause_graph = true;
            else
                pause_graph = false;
        }

     

        private void limparGráficoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grafico.Series[0].Points.Clear();
            grafico.Series[1].Points.Clear();
            grafico.Series[2].Points.Clear();
            grafico.ResetAutoValues();
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(serialPort1.IsOpen)
            {
                serialPort1.Close();
                serialPort1.Dispose();
            }
        }

        private void AtualizaControlador()
        {
            rec_dadoscont = true;
            byte crc = 0;
            for (int i = 0; i < 27; i++)
                crc ^= dados_cont[i];
            if (crc == 0)
            {
                label18.Text = "u[k] = ";
                for (int i = 0; i < 5; i++)
                {
                    double temp_s = (double)(BitConverter.ToInt16(dados_cont, i * 2)) / 1000;
                    if (temp_s < 0)
                        label18.Text = label18.Text + " - " + (temp_s * -1).ToString("0.000") + "u[k-" + (i + 1).ToString() + "]";
                    if ((temp_s > 0) && (i > 0))
                        label18.Text = label18.Text + " + " + temp_s.ToString("0.000") + "u[k-" + (i + 1).ToString() + "]";
                    else if ((temp_s > 0) && (i == 0))
                        label18.Text = label18.Text + temp_s.ToString("0.000") + "u[k-" + (i + 1).ToString() + "]";
                }
                for (int i = 5; i < 11; i++)
                {
                    double temp_s = (double)(BitConverter.ToInt16(dados_cont, i * 2)) / 1000;
                    if (i != 5) { 
                    if (temp_s < 0)
                        label18.Text = label18.Text + " - " + (temp_s * -1).ToString("0.000") + "e[k-" + (i - 5).ToString() + "]";
                    if (temp_s > 0)
                        label18.Text = label18.Text + " + " + temp_s.ToString("0.000") + "e[k-" + (i - 5).ToString() + "]";
                    } else { 
                    if (temp_s < 0)
                        label18.Text = label18.Text + " - " + (temp_s * -1).ToString("0.000") + "e[k]";
                    if (temp_s > 0)
                        label18.Text = label18.Text + " + " + temp_s.ToString("0.000") + "e[k]";
                    }

                }

                groupBox8.Text = "Controlador(D). Ts: " + (BitConverter.ToInt16(dados_cont, 22)).ToString() + " ms, DB: " + ((double)BitConverter.ToInt16(dados_cont, 24) / 100).ToString("0.00") + " V / mA";
            }
        }
    }
}
