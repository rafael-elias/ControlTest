 /* Programa do Arduino da Placa de Aquisição de dados e Controle
  * Plataforma Didática para implementação e avaliação de controladores discretos
  * Trabalho de Conclusão de Curso de Engenharia de Controle Automação
  * Ifes - Campus Serra
  * Autor: Rafael Elias de Sousa
  * Orientador: Prof. Dr. Saul Munareto*/
 
#include <EEPROM.h>
#define CS 10
#define SCK 11
#define SDI 12
#define PIN_V A2
#define PIN_I A6

byte dados_PC[14];
byte dados_eeprom[30];
byte dados_ARD[6];
byte dados_cont[27];
byte estado = 80;
double c_duk[5] = {0,0,0,0,0};
double c_errok[6] =  {0,0,0,0,0,0};
double duk[6] = {0,0,0,0,0,0};
double errok[6] = {0,0,0,0,0,0};
double uk = 0, setpoint = 0, saida_c_MA = 0, deadband = 0, an=0;
boolean feedback_on = false, med_tensao_corr = false;
unsigned long t_aq=100,t_amost=100, last_t_amos = 0, last_t_aq = 10;

//Configurações
const int saida_maxima = 1000;
const int conf = 0b0011000000000000;             //16º bit: A=0,B=1; 15º: Bypass Buffer; 14º: Ganho; 13º: Shutdown hab/ 12 a 1: mascara para os dados

//Leitura dos dados da EEPROM 
void lere2prom() {
  byte crc =0;
  for(int i=0; i<=29; i++) {
    dados_eeprom[i] = EEPROM.read(i);     
    crc ^= dados_eeprom[i];
    }
  if(crc==0) {
    for(int i=0; i<5; i++)
      c_duk[i] = (double)((dados_eeprom[i*2]) | (dados_eeprom[i*2+1] << 8))/1000.0;
    for(int i=5; i<11; i++)   
     c_errok[i-5] = (double)((dados_eeprom[i*2]) | (dados_eeprom[i*2+1] << 8))/1000.0;
    setpoint = ((dados_eeprom[22]) | (dados_eeprom[23] << 8));
    deadband = ((dados_eeprom[24]) | (dados_eeprom[25] << 8));
    t_amost =  ((dados_eeprom[26]) | (dados_eeprom[27] << 8));
    estado = dados_eeprom[28];
  }
  if (!((estado >= 80)&(estado <=83)))
  estado=80;

}

void setup() {
pinMode(CS,OUTPUT);
pinMode(SCK,OUTPUT);
pinMode(SDI,OUTPUT);
pinMode(PIN_V,INPUT);
pinMode(PIN_I,INPUT);
analogReference(EXTERNAL);
digitalWrite(CS,HIGH);
digitalWrite(SCK, LOW);
Serial.begin(115200);
byte modo_aut = EEPROM.read(35);
if (modo_aut == 33)
lere2prom();
if (estado >= 82)
  feedback_on = true;
if ((estado == 81) | (estado == 83))
  med_tensao_corr = true;
last_t_amos = millis();
}

void loop() {
 
    if((millis() - last_t_amos) >= t_amost) {
        last_t_amos = millis();
        if (!med_tensao_corr) { //1 = corrente
         double an2 = analogRead(PIN_V)*(double(1000)/1023);
         controlador(an2);
        }
        else
        {
         double an1 = analogRead(PIN_I)*(double(2000)/1023);
         controlador(an1);
        }
    }
        if((millis() - last_t_aq) >= t_aq) {
        last_t_aq = millis();
        if (!med_tensao_corr)
        an = analogRead(PIN_V)*(double(1000)/1023);
        else
        an = analogRead(PIN_I)*(double(2000)/1023);
        dados_ARD[0]=estado;                                                                 //ID Começo e tipo do pacote
        dados_ARD[1]=(byte)(int)an;                                                          //LSB AN1
        dados_ARD[2]=(byte)((int)an >> 8);                                                   //MSB AN1     
        dados_ARD[3]=(byte)((int)uk);                                                          //LSB UK
        dados_ARD[4]=(byte)((int)uk >> 8);                                                   //MSB UK
        dados_ARD[5]=(dados_ARD[1] ^ dados_ARD[2] ^ dados_ARD[3] ^ dados_ARD[4]);    //CRC
        Serial.write(dados_ARD,6); 
      }
  if(Serial.available()) {
    dados_PC[0]= Serial.read();
    delay(4);                                                                            //Aguarda o restante dos dados (Não interrompe o recebimento da serial)
        if ((dados_PC[0] >= 65) & (dados_PC[0] <= 69))  {                               //Dados com 2 bytes + crc
          for(int i=1; i<4; i++) {
          dados_PC[i] = Serial.read();
          }
            if (((dados_PC[1] ^ dados_PC[2]) ^ dados_PC[3]) == 0) {                      //Checa CRC
              if (dados_PC[0] == 65) {                                                   //Tempo de aquisição supervisório
              int t_temp = ((dados_PC[1]) | (dados_PC[2] << 8));                         //Combina bytes
                if (t_temp > 1) t_aq=t_temp;
              }
              if (dados_PC[0] == 66) {                                                     //MV (malha aberta)
              int tensao = (dados_PC[1]) | (dados_PC[2] << 8);  
                if (tensao <= saida_maxima) {
                  saida_c_MA = tensao;
                  uk = tensao;
					        envia_spi(tensao);
                }
              }
              if (dados_PC[0] == 67) {                                                    //Setpoint
             int sets = ((dados_PC[1]) | (dados_PC[2] << 8));
             if (sets >= 0) {        
             setpoint = sets; 
           //  last_t_amos-=t_amost;       //Força atualização do controlador
               }
            }
                if (dados_PC[0] == 68) {                                                  //Ts
              int tempo = ((dados_PC[1]) | (dados_PC[2] << 8));                        
                if ((tempo >= 1) & (tempo < 4096))  {                                               
                t_amost=tempo;
                }
              }

            if (dados_PC[0] == 69) {                                                        //Dead Band
             deadband = ((dados_PC[1]) | (dados_PC[2] << 8));
             if (deadband < 0) deadband = 0;
             if (deadband > 2000) deadband = 2000;
              }
        }   
    } 
            if (dados_PC[0] == 70) {                                                       //Coeficientes uk
               byte CRC = 0;
               for(int i=1; i<12; i++) {
                dados_PC[i] = Serial.read();
                CRC ^= dados_PC[i];
                 }
               if (CRC == 0) {
                for(int i=0; i<5; i++)
                c_duk[i] = (double)((dados_PC[i*2+1]) | (dados_PC[i*2+2] << 8))/1000.0;
                }
              }
             if (dados_PC[0] == 71) {                                                 //Coeficientes ek
              byte CRC = 0;
                 for(int i=1; i<14; i++) {
                dados_PC[i] = Serial.read();
                CRC ^= dados_PC[i];
                 }
               if (CRC == 0) {
               for(int i=0; i<6; i++)   
               c_errok[i] = (double)((dados_PC[i*2+1]) | (dados_PC[i*2+2] << 8))/1000.0;
                                          
              }
             }
         if (dados_PC[0] == 72) {                                                           //Abre ou fecha malha
            dados_PC[1] = Serial.read();
            dados_PC[2] = Serial.read();
            if((dados_PC[1] ^ dados_PC[2]) == 0) {
                if (dados_PC[1] == 1){
                   feedback_on = true; 
                   estado |= 2; 
                   }              
                else {
                  feedback_on = false;
                  estado &= 253; 
                }
             }
         }  
           if (dados_PC[0] == 73) {                                                       //Uso da entrada de tensão ou corrente
            dados_PC[1] = Serial.read();
            dados_PC[2] = Serial.read();
            if((dados_PC[1] ^ dados_PC[2]) == 0) {
                if (dados_PC[1] == 1) {
                  med_tensao_corr = true; 
                  estado |= 1; 
               }              
                else {
                  med_tensao_corr = false; 
                  estado &= 254; 
                }     
            }
         }
         if (dados_PC[0] == 74) {                                                         //Grava eeprom e habilita modo autonomo;
            dados_PC[1] = Serial.read();
            dados_PC[2] = Serial.read();
            if((dados_PC[1] ^ dados_PC[2]) == 0) {
                if (dados_PC[1] == 1) {
                  gravae2prom();
                  EEPROM.update(35,33);
                }
                
            }
         }
         if (dados_PC[0] == 75) {                                                         //Desabilita modo autonomo
            dados_PC[1] = Serial.read();
            dados_PC[2] = Serial.read();
            if((dados_PC[1] ^ dados_PC[2]) == 0) {
                if (dados_PC[1] == 1) 
                EEPROM.update(35,55); 
            }
         }
         if (dados_PC[0] == 76) {                                                         //Requisição envio dados do controlador
            dados_PC[1] = Serial.read();
            dados_PC[2] = Serial.read();
            if((dados_PC[1] ^ dados_PC[2]) == 0) {
                envia_dadoscontrol();
            }
         }
  }
 }
  
 void controlador(double yk) {			                      //Recebe valor de feedback
    errok[0] = setpoint - yk;                             //Erro
    if (abs(errok[0]) < deadband) errok[0] = 0;           //Dead band
    duk[0] = (c_errok[5]*errok[5]);
    for (int i=0;i<5;i++){
    duk[0] += (c_duk[i]*duk[i+1] + c_errok[i]*errok[i]);
    }
    uk=duk[0];
    if (uk < 0) uk=0;  
    if (uk > saida_maxima) uk=saida_maxima; 
    if (!feedback_on){                                      //Se malha aberta
     uk=saida_c_MA;
     setpoint = yk;
     errok[0]=0;
    }
    envia_spi((int)uk);                                     //Atualiza saída
    for (int i=5;i>1;i--) {                                 //Desloca vetor duk
    duk[i] = duk[i-1];
    }
    duk[1] = uk;
    for (int i=5;i>0;i--) {                                 //Desloca vetor errok
    errok[i] = errok[i-1];
    }
   saida_c_MA = uk;            //Saída malha aberta
   }

//Envia o valor ao conversor D/A MCP4922 via SPI

void envia_spi(int valor) {
    valor *= 4.095;
	  digitalWrite(CS,LOW);                         //digitalWrite (delay intrínseco). Inicia transmissão.
    int dado = (valor | conf);
    for(int i=15;i>-1;i--){
        if (dado & (1<<i))                        //Transfere bit do dado para pino SDI
        PORTB |= B00010000;                       //SET SDI ( PB4 - Pino 12)
        else
        PORTB &= ~(B00010000);                    //RESET SDI ( PB4 - Pino 12)
        PORTB |= B00001000;                       //SET SCK (PB3 - Pino 11)  
        PORTB &= ~(B00001000);                    //RESET SCK (PB3 - Pino 11)
    }
digitalWrite(CS,HIGH);
    
  }


//Envia para o supervisório o controlador que está configurado no momento
void envia_dadoscontrol() {
  dados_cont[0]=84;               //ID Pacote
  dados_cont[5]=0;                //CRC
  dados_cont[26]=0;               //CRC
    for(int i=1; i<5; i++) {
    dados_cont[i] = i+3;
    dados_cont[5] ^= dados_cont[i];  
  }
    Serial.write(dados_cont,6);
    delay(2);
    for(int i=0; i<5; i++) {
     dados_cont[i*2]=(byte)((int)(c_duk[i]*1000));
    dados_cont[i*2+1]=(byte)((int)(c_duk[i]*1000) >> 8);
  }
    for(int i=5; i<11; i++) {  
    dados_cont[i*2]=(byte)((int)(c_errok[i-5]*1000));
    dados_cont[i*2+1]=(byte)((int)(c_errok[i-5]*1000) >> 8);
    }
    dados_cont[22] = (byte)(t_amost);
    dados_cont[23] = (byte)(t_amost >> 8);
    dados_cont[24] = (byte)((int)deadband);
    dados_cont[25] = (byte)((int)deadband >> 8);
     for(int i=0; i<26; i++) 
      dados_cont[26] ^= dados_cont[i];  
    Serial.write(dados_cont,27);
}

//Grava dados na EEPROM
void gravae2prom() {
  byte crc =0;
    for(int i=0; i<5; i++) {
    dados_eeprom[i*2] = (byte)((int)(c_duk[i]*1000));
    dados_eeprom[i*2+1] = (byte)((int)(c_duk[i]*1000) >> 8);
    }
    for(int i=5; i<11; i++) {  
    dados_eeprom[i*2] = (byte)((int)(c_errok[i-5]*1000));
    dados_eeprom[i*2+1] = (byte)((int)(c_errok[i-5]*1000) >> 8);
    }
    dados_eeprom[22] = (byte)((int)setpoint);
    dados_eeprom[23] = (byte)((int)setpoint >> 8);
    dados_eeprom[24] = (byte)((int)deadband);
    dados_eeprom[25] = (byte)((int)deadband >> 8);
    dados_eeprom[26] = (byte)(t_amost);
    dados_eeprom[27] = (byte)(t_amost >> 8);
    dados_eeprom[28] = estado;
    for(int i=0; i<29; i++) 
      crc ^= dados_eeprom[i];    
    dados_eeprom[29] = crc;
      for (int i=0;i<30;i++)
    EEPROM.update(i,dados_eeprom[i]);
        
  }
