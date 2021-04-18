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
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;

namespace UniControl
{
    public partial class Config_Control : Form
    {
        List<string> coeficientes = new List<string> { };
        List<string> datalst = new List<string> { };
        List<TextBox> txtsbox = new List<TextBox>();
        string datatx;
        private SerialPort PortaSerial;
        bool loaded = false;

      public Config_Control(SerialPort PortaS)
        {
            InitializeComponent();
            txtsbox.Add(cftxt1);
            txtsbox.Add(cftxt2);
            txtsbox.Add(cftxt3);
            txtsbox.Add(cftxt4);
            txtsbox.Add(cftxt5);
            txtsbox.Add(cftxt6);
            txtsbox.Add(cftxt7);
            txtsbox.Add(cftxt8);
            txtsbox.Add(cftxt9);
            txtsbox.Add(cftxt10);
            txtsbox.Add(cftxt11);
            this.PortaSerial = PortaS;
            
          
          }

        private void Recarrega()
        {
            if (File.Exists("cntrls.cnt"))
            {
                datatx = System.IO.File.ReadAllText("cntrls.cnt");
                datalst = new List<string>(System.IO.File.ReadAllLines("cntrls.cnt"));
                int linhas = datatx.Split('\n').Length - 1;
                comboBox1.Items.Clear();
                coeficientes.Clear();
                // string[] variables = data.Split(';','\n'); //Split('\n')[0].
                for (int i = 0; i < linhas; i++)
                {
                    string[] temp = datatx.Split('\n')[i].Split(';');
                    if (temp.Length == 16)
                    {
                        if (!String.IsNullOrWhiteSpace(temp[0]))
                        {
                            comboBox1.Items.Add(temp[0]);
                            List<string> lcoefs = new List<string>(temp);
                            lcoefs.RemoveAt(0);
                            coeficientes.AddRange(lcoefs);
                        }
                    }
                        
                }

            }
         
        }

private void Config_Control_Load(object sender, EventArgs e)
        {
            if (!loaded)
            {
                Recarrega();
                comboBox2.SelectedIndex = 0;
                comboBox3.SelectedIndex = 0;
                loaded = true;
            }
            

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //int j = Array.FindIndex(datalst, row => row.Contains(comboBox1.Text))*11;
            if (comboBox1.SelectedIndex >= 0)
            {
                int j = (comboBox1.SelectedIndex) * 15;
                try
                {
                    cftxt1.Text = coeficientes[j];
                    cftxt2.Text = coeficientes[j + 1];
                    cftxt3.Text = coeficientes[j + 2];
                    cftxt4.Text = coeficientes[j + 3];
                    cftxt5.Text = coeficientes[j + 4];
                    cftxt6.Text = coeficientes[j + 5];
                    cftxt7.Text = coeficientes[j + 6];
                    cftxt8.Text = coeficientes[j + 7];
                    cftxt9.Text = coeficientes[j + 8];
                    cftxt10.Text = coeficientes[j + 9];
                    cftxt11.Text = coeficientes[j + 10];
                    cnttstxt.Text = coeficientes[j + 11];
                    deadbandtxt.Text = coeficientes[j + 12];
                    if (!(coeficientes[j + 13].Contains("Tensão")))
                        comboBox2.SelectedIndex = 1;
                    else
                        comboBox2.SelectedIndex = 0;
                    if (!(coeficientes[j + 14].Contains("Aberta")))
                        comboBox3.SelectedIndex = 1;
                    else
                        comboBox3.SelectedIndex = 0;
                    cnt_nametxt.Text = comboBox1.Text;
                }
                catch
                {
                    MessageBox.Show("Erro ao processar os dados.");
                    return;
                }
            }
        }

        private void deleta(int indice)
        {
            datalst.RemoveAt(indice);
            File.Delete("cntrls.cnt");
            using (StreamWriter delarq = new StreamWriter("cntrls.cnt"))
            {
                foreach (string s in datalst)
                {
                    if (!String.IsNullOrWhiteSpace(s))
                    {
                        delarq.WriteLine(s);
                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if ((comboBox1.Items.Count > 0) & (comboBox1.SelectedIndex != -1)) 
                deleta(comboBox1.SelectedIndex);
             Recarrega();
                if (comboBox1.Items.Count > 0)
                    comboBox1.SelectedIndex = 0;

            
        }
            
        private void button2_Click(object sender, EventArgs e)
        {
            if ((comboBox1.Text == cnt_nametxt.Text) & (cnt_nametxt.Text != ""))
            {
                deleta(comboBox1.SelectedIndex);
                comboBox1.Items.Clear();

            }
            if (!(comboBox1.Items.Contains(cnt_nametxt.Text.Trim()) | (cnt_nametxt.Text == "")))
            {
                using (StreamWriter cnts_arq = new StreamWriter("cntrls.cnt", true))
                    cnts_arq.WriteLine(cnt_nametxt.Text + ";" + cftxt1.Text + ";" + cftxt2.Text + ";" + cftxt3.Text + ";" + cftxt4.Text + ";" + cftxt5.Text + ";"
                    + cftxt6.Text + ";" + cftxt7.Text + ";" + cftxt8.Text + ";" + cftxt9.Text + ";" + cftxt10.Text + ";" + cftxt11.Text + ";"+ cnttstxt.Text + ";" + deadbandtxt.Text + ";" + comboBox2.Text + ";" + comboBox3.Text);
                    Recarrega();
                    comboBox1.SelectedIndex = comboBox1.Items.Count - 1;

            } else
                MessageBox.Show("Nome do controlador já existe ou está vazio.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);


        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (PortaSerial.IsOpen) { 
            byte i = 1;
            byte[] temp = {70,0,0,0,0,0,0,0,0,0,0,0,71,0,0,0,0,0,0,0,0,0,0,0,0,0};      //Mascara parâmetros controlador
            foreach (TextBox ttcfcs in txtsbox) {
                int cfmult = (int)(Math.Round(Double.Parse(ttcfcs.Text),3)*1000);
                temp[i] = (byte)(cfmult);
                temp[i+1] = (byte)(cfmult >> 8);
                    i += 2;
                        if (i == 11)
                {
                    for (int j = 1; j < 11; j++)
                        temp[i] ^= temp[j]; //crc
                    i += 2;
                }
                if (i == 25)
                {
                    for (int j = 13; j < 25; j++)
                        temp[i] ^= temp[j]; //crc
                    break;
                }
              
            }
                try { PortaSerial.Write(temp, 0, 26); }
                    catch { MessageBox.Show("Erro ao enviar dados.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); return; }

                int ttsdb = int.Parse(cnttstxt.Text);
                byte[] ttsdb_ar = { 68, 0, 0, 0, 69, 0, 0, 0 }; //Mascara Ts e DB
              
                //Período de amostragem
                ttsdb_ar[1] = (byte)(ttsdb);
                ttsdb_ar[2] = (byte)(ttsdb >> 8);
                ttsdb_ar[3] = (byte)(ttsdb_ar[1] ^ ttsdb_ar[2]);
                           
                //Deadband
                ttsdb = (int)(Math.Round(Double.Parse(deadbandtxt.Text), 2) * 100);
                ttsdb_ar[5] = (byte)(ttsdb);
                ttsdb_ar[6] = (byte)(ttsdb >> 8);
                ttsdb_ar[7] = (byte)(ttsdb_ar[5] ^ ttsdb_ar[6]);

                byte[] malha_fb = { 72, 0, 0, 73, 0, 0, 76, 1, 1 };   //Mascara feedback, tesão / corrente. Ao final envia requisição dos dados do controlador

                //Malha aberta ou fechada. Parâmetros iniciais.
                malha_fb[1] = (byte)comboBox3.SelectedIndex;
                malha_fb[2] = malha_fb[1];
           
                //Entrada tensão ou corrente
                malha_fb[4] = (byte)comboBox2.SelectedIndex;
                malha_fb[5] = malha_fb[1];
           
                try {
                    PortaSerial.Write(ttsdb_ar, 0, 8);
                    PortaSerial.Write(malha_fb, 0, 9);
                     }
                catch { MessageBox.Show("Erro ao enviar dados.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); return; }
                this.Close();

            }
        }


        private void button5_Click(object sender, EventArgs e)
        {

        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox2.SelectedIndex == 0)
                label13.Text = "V";
            else label13.Text = "mA";
        }

        private void deadbandtxt_Leave(object sender, EventArgs e)
        {
            double db_temp = double.Parse(deadbandtxt.Text);
            if (comboBox2.SelectedIndex == 0)
            {
                if (db_temp > 10)
                    deadbandtxt.Text = "10";
            } else if (db_temp > 20)
                deadbandtxt.Text = "20";
            if (db_temp < 0)
                deadbandtxt.Text = "0";
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            cftxt1.Text = "0,000";
            cftxt2.Text = "0,000";
            cftxt3.Text = "0,000";
            cftxt4.Text = "0,000";
            cftxt5.Text = "0,000";
            cftxt6.Text = "0,000";
            cftxt7.Text = "0,000";
            cftxt8.Text = "0,000";
            cftxt9.Text = "0,000";
            cftxt10.Text = "0,000";
            cftxt11.Text = "0,000";
            deadbandtxt.Text = "0,00";
            cnttstxt.Text = "0";
            cnt_nametxt.Text = "";
            comboBox1.SelectedIndex = -1;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
          
        }

    }
        
}
