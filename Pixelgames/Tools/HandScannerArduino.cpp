/**
 * API @115200 baud
 * Message structure:
 * <" "><MESSAGE><NL>
 * (NL termination, and start of packet is empty space)
 */

#include <Arduino.h>
#include <TimerOne.h>
#include <Adafruit_NeoPixel.h>
#include <SPI.h>      //Library  RC522 Module uses SPI protocol
#include <MFRC522.h> //Library  RC522 Module

#define UART_BAUD 115200
#define TIMER_CTR_100MS 50000 //0.05s
#define BLINK_ON_OFF_TIMEOUT_CNT 10 //appx 0.5s

//RFID related:
#define SS_PIN 10 //SDA Pin 
#define RST_PIN 2 //RST Pin 
MFRC522 mfrc522(SS_PIN, RST_PIN);  // Create MFRC522 instance.  
byte g_readCard[4];   // Stores scanned ID read from RFID Module

int getID();

// #define DEBUG_MSG

//Ring LEDs
#define NEOPIX_PIN_1 4
#define MAX_LEDS_RING 24
Adafruit_NeoPixel strip_ring = Adafruit_NeoPixel(MAX_LEDS_RING, NEOPIX_PIN_1, NEO_GRB + NEO_KHZ800);

//Bg LEDs
#define NEOPIX_PIN_2 A0
#define MAX_LEDS_BG 50
Adafruit_NeoPixel strip_bg = Adafruit_NeoPixel(MAX_LEDS_BG, NEOPIX_PIN_2, NEO_GRB + NEO_KHZ800);

//center arrow LEDs
#define NEOPIX_PIN_3 6
#define MAX_LEDS_ARROW 23
Adafruit_NeoPixel strip_arrow = Adafruit_NeoPixel(MAX_LEDS_ARROW, NEOPIX_PIN_3, NEO_GRB + NEO_KHZ800);

//Side Progress LEDs
#define NEOPIX_PIN_4 5
#define MAX_LEDS_PROG 23
Adafruit_NeoPixel strip_progress = Adafruit_NeoPixel(MAX_LEDS_PROG, NEOPIX_PIN_4, NEO_GRB + NEO_KHZ800);

volatile int  g_blinkCntr=0;
void SetNeopix(int state);

String g_InputString1;
volatile bool g_bSerLock1=false, g_bStringComplete1=false, g_bClearOneCmd1=false;
volatile bool g_bBlinkActivated=false;
void ProcessComm();

volatile int g_blinkTimer=0, g_currRingIndex=0;
volatile bool g_bBgLedStatus=false, g_bBlinkOnceRing=false, g_bAllOff=false;

volatile int g_playerLevel=0, g_currArrowIndex=0;

#define MAX_PLAYERS 5
const int c_progLvl[MAX_PLAYERS]={3,5,5,5,5};

#define MAX_ARROW_SEGMENTS 5
const int c_arrowLvl[MAX_ARROW_SEGMENTS]={3,5,5,5,5};

void timerisr();
void POST();
void AllOff();

void setup() {

  g_InputString1.reserve(512);
  Serial.begin(UART_BAUD); //comm. port: Rpi
  delay(100);
  SPI.begin();           // MFRC522 Hardware uses SPI protocol
  mfrc522.PCD_Init();    // Initialize MFRC522 Hardware
  mfrc522.PCD_SetAntennaGain(mfrc522.RxGain_max);
  delay(100);

  strip_arrow.begin(); 
  delay(10);
  strip_arrow.show(); 
  delay(10);
  strip_bg.begin(); 
  delay(10);
  strip_bg.show(); 
  delay(10);
  strip_ring.begin(); 
  delay(10);
  strip_ring.show(); 
  delay(10);
  strip_progress.begin(); 
  delay(10);
  strip_progress.show(); 
  delay(500);
  POST();
  Timer1.initialize(TIMER_CTR_100MS); 
  Timer1.attachInterrupt(timerisr);
}

void loop() {
  getID();
}


void POST()
{
  for(int j=0; j<MAX_LEDS_ARROW; j++)
  {
    strip_arrow.setPixelColor(j, strip_arrow.Color(255, 0, 0)); //R
    strip_arrow.show(); 
  }
  for(int j=0; j<MAX_LEDS_RING; j++)
  {
    strip_ring.setPixelColor(j, strip_ring.Color(255, 0, 0)); //R
    strip_ring.show(); 
  }
  for(int j=0; j<MAX_LEDS_PROG; j++)
  {
    strip_progress.setPixelColor(j, strip_progress.Color(255, 0, 0)); //R
    strip_progress.show(); 
  }
  for(int j=0; j<MAX_LEDS_BG; j++)
  {
    strip_bg.setPixelColor(j, strip_bg.Color(255, 0, 0)); //R
    strip_bg.show(); 
  }
  delay(200);



  for(int j=0; j<MAX_LEDS_ARROW; j++)
  {
    strip_arrow.setPixelColor(j, strip_arrow.Color(0, 255, 0)); //G
    strip_arrow.show(); 
  }
  for(int j=0; j<MAX_LEDS_RING; j++)
  {
    strip_ring.setPixelColor(j, strip_ring.Color(0, 255, 0)); //G
    strip_ring.show(); 
  }
  for(int j=0; j<MAX_LEDS_PROG; j++)
  {
    strip_progress.setPixelColor(j, strip_progress.Color(0, 255, 0)); //G
    strip_progress.show(); 
  }
  for(int j=0; j<MAX_LEDS_BG; j++)
  {
    strip_bg.setPixelColor(j, strip_bg.Color(0, 255, 0)); //G
    strip_bg.show(); 
  }
  delay(200);


  for(int j=0; j<MAX_LEDS_ARROW; j++)
  {
    strip_arrow.setPixelColor(j, strip_arrow.Color(0, 0, 255)); //B
    strip_arrow.show(); 
  }
  for(int j=0; j<MAX_LEDS_RING; j++)
  {
    strip_ring.setPixelColor(j, strip_ring.Color(0, 0, 255)); //B
    strip_ring.show(); 
  }
  for(int j=0; j<MAX_LEDS_PROG; j++)
  {
    strip_progress.setPixelColor(j, strip_progress.Color(0, 0, 255)); //B
    strip_progress.show(); 
  }
  for(int j=0; j<MAX_LEDS_BG; j++)
  {
    strip_bg.setPixelColor(j, strip_bg.Color(0, 0, 255)); //B
    strip_bg.show(); 
  }

delay(200);
    for(int j=0; j<MAX_LEDS_ARROW; j++)
  {
    strip_arrow.setPixelColor(j, strip_arrow.Color(255, 255, 255)); //W
    strip_arrow.show(); 
  }
  for(int j=0; j<MAX_LEDS_RING; j++)
  {
    strip_ring.setPixelColor(j, strip_ring.Color(255, 255, 255)); //W
    strip_ring.show(); 
  }
  for(int j=0; j<MAX_LEDS_PROG; j++)
  {
    strip_progress.setPixelColor(j, strip_progress.Color(255, 255, 255)); //W
    strip_progress.show(); 
  }
  for(int j=0; j<MAX_LEDS_BG; j++)
  {
    strip_bg.setPixelColor(j, strip_bg.Color(255, 255, 255)); //W
    strip_bg.show(); 
  }
  delay(200);


  for(int j=0; j<MAX_LEDS_ARROW; j++)
  {
    strip_arrow.setPixelColor(j, strip_arrow.Color(0, 0, 0)); //0
    strip_arrow.show(); 
  }
  for(int j=0; j<MAX_LEDS_RING; j++)
  {
    strip_ring.setPixelColor(j, strip_ring.Color(0, 0, 0)); //0
    strip_ring.show(); 
  }
  for(int j=0; j<MAX_LEDS_PROG; j++)
  {
    strip_progress.setPixelColor(j, strip_progress.Color(0, 0, 0)); //0
    strip_progress.show(); 
  }
  for(int j=0; j<MAX_LEDS_BG; j++)
  {
    strip_bg.setPixelColor(j, strip_bg.Color(0, 0, 0)); //0
    strip_bg.show(); 
  }
  
}


int getID() 
{
  // Getting ready for Reading PICCs
  if ( ! mfrc522.PICC_IsNewCardPresent()) { //If a new PICC placed to RFID reader continue
    return 0;
  }
  if ( ! mfrc522.PICC_ReadCardSerial()) { //Since a PICC placed get Serial and continue
    return 0;
  }
  // There are Mifare PICCs which have 4 byte or 7 byte UID care if you use 7 byte PICC
  // I think we should assume every PICC as they have 4 byte UID
  // Until we support 7 byte PICCs

#ifdef DEBUG
  Serial.println("Scanning PICC's UID.........");
#endif
  for (int i = 0; i < mfrc522.uid.size; i++) 
  {  //
    g_readCard[i] = mfrc522.uid.uidByte[i];
// #ifdef DEBUG_RFID
    if(g_readCard[i]<=16)
      Serial.print("0");
    Serial.print(g_readCard[i], HEX);
// #endif
  }
   Serial.println();
#ifdef DEBUG
  Serial.println("");
#endif
  mfrc522.PICC_HaltA(); // Stop reading
  return 1;
}


void timerisr()
{
  
  if(g_bAllOff==false)
  {

    {
      if(g_currRingIndex==MAX_LEDS_RING)
          g_currRingIndex=0;
      for(int j=0; j<MAX_LEDS_RING; j++)
      {
          strip_ring.setPixelColor(j, strip_ring.Color(0, 0, 0)); 
      }
      strip_ring.setPixelColor(g_currRingIndex, strip_ring.Color(0, 0, 255)); 
      if(g_currRingIndex==0)
        strip_ring.setPixelColor(MAX_LEDS_RING-1, strip_ring.Color(5, 0, 120)); 
      else
        strip_ring.setPixelColor(g_currRingIndex-1, strip_ring.Color(5, 0, 120)); 
      g_currRingIndex++;
    }
    strip_ring.show(); 

    {
      if(g_currArrowIndex<0)
          g_currArrowIndex=MAX_ARROW_SEGMENTS;
      for(int j=0; j<MAX_LEDS_PROG; j++)
      {
          strip_arrow.setPixelColor(j, strip_arrow.Color(0, 0, 0)); 
      }
      int cntStart=0;
      for(int i=0; i<g_currArrowIndex; i++)
        cntStart+=c_arrowLvl[i];
      for(int i=cntStart; i<cntStart+c_arrowLvl[g_currArrowIndex]; i++)
        strip_arrow.setPixelColor(i,strip_arrow.Color(0, 0, 255));
      g_currArrowIndex--;
    }
    strip_arrow.show();





    
    if(g_blinkTimer<BLINK_ON_OFF_TIMEOUT_CNT)
      g_blinkTimer++;
    else{
      if(g_bBlinkActivated==true)
      {
        if(g_bBgLedStatus==false)
          {
              for(int j=0; j<MAX_LEDS_BG; j++)
              {
                  strip_bg.setPixelColor(j, strip_bg.Color(0, 0, 255)); 
              }
              strip_bg.show(); 
              g_bBgLedStatus=true;
          }
        else
        {
              for(int j=0; j<MAX_LEDS_BG; j++)
              {
                  strip_bg.setPixelColor(j, strip_bg.Color(0, 0, 0)); 
              }
              strip_bg.show(); 
              g_bBgLedStatus=false;
        }
      g_blinkTimer=0; 
      }    
    }
  }
  
}


void serialEvent()
{
    while ((Serial.available()) && (g_bSerLock1==false) )
    {
        char inChar = (char)Serial.read();
        if (g_InputString1.length()<512)
        {
            g_InputString1 += inChar;
            if (inChar == '\n')
            {
                g_bStringComplete1 = true;
                ProcessComm();
            }
        }
        else
            g_bClearOneCmd1=true;
    }
}


void ProcessComm()
{
  if (g_bStringComplete1)
  {
      g_bSerLock1 = true;
      if((g_InputString1.indexOf(" ")==0)) //if first character is START_OF_PACKET, else clear till 1st NL
      {
        g_InputString1.remove(0,1);
#ifdef DEBUG_MSG
          Serial.println("D::in g_InputString.indexOf(test_1) entered test loop (1)");
#endif
          if(g_InputString1.indexOf("CONN")==0)
          {
            
            //Serial.println("ACK");
            Serial.println("ACKCO");
#ifdef DEBUG_MSG
          Serial.print("D:: Received CONN");
#endif
            g_InputString1.remove(0, (g_InputString1.indexOf('\n'))+1);
          }




          else if(g_InputString1.indexOf("L_LVLB_")==0) //Sets number of players in format: L_LVL3 = 3 levels on
          {
            g_bAllOff=false;
            String incomingLvl=g_InputString1.substring(7,g_InputString1.indexOf('\n'));
            g_playerLevel = incomingLvl.toInt();
            for(int j=0; j<MAX_LEDS_PROG; j++)
            {
                strip_progress.setPixelColor(j, strip_progress.Color(0, 0, 0)); 
            }
            strip_progress.show();
            int cnt=0;
            for (int i=0; i<g_playerLevel; i++)
              cnt+=c_progLvl[i];
            for(int j=0; j<cnt; j++)
            {
                strip_progress.setPixelColor(j, strip_progress.Color(0, 0, 255)); 
            }
            strip_progress.show();
            
#ifdef DEBUG_MSG
            Serial.print("D::Laser Info Received");
#endif
            g_InputString1.remove(0, (g_InputString1.indexOf('\n'))+1);
          } 
          else if(g_InputString1.indexOf("L_LVLG_")==0) //Sets number of players in format: L_LVL3 = 3 levels on
          {
            g_bAllOff=false;
            String incomingLvl=g_InputString1.substring(7,g_InputString1.indexOf('\n'));
            g_playerLevel = incomingLvl.toInt();
            for(int j=0; j<MAX_LEDS_PROG; j++)
            {
                strip_progress.setPixelColor(j, strip_progress.Color(0, 0, 0)); 
            }
            strip_progress.show();
            int cnt=0;
            for (int i=0; i<g_playerLevel; i++)
              cnt+=c_progLvl[i];
            for(int j=0; j<cnt; j++)
            {
                strip_progress.setPixelColor(j, strip_progress.Color(0, 255, 0)); 
            }
            strip_progress.show();
            
#ifdef DEBUG_MSG
            Serial.print("D::Laser Info Received");
#endif
            g_InputString1.remove(0, (g_InputString1.indexOf('\n'))+1);
          } 




          else if(g_InputString1.indexOf("L_BG_1")==0) //blink blue!
          {
            g_bAllOff=false;
            g_bBlinkActivated=true;
#ifdef DEBUG_MSG
            Serial.print("D::Laser Info Received");
#endif
            g_InputString1.remove(0, (g_InputString1.indexOf('\n'))+1);
          }    
          else if(g_InputString1.indexOf("L_BG_G")==0) //GREEN
          {
            g_bAllOff=false;
            g_bBlinkActivated=false;
            delay(200);
            for(int j=0; j<MAX_LEDS_BG; j++)
            {
                strip_bg.setPixelColor(j, strip_bg.Color(0, 0, 0)); 
            }
            strip_bg.show();
            for(int j=0; j<MAX_LEDS_BG; j++)
            {
                strip_bg.setPixelColor(j, strip_bg.Color(0, 255, 0)); 
            }
            strip_bg.show();
#ifdef DEBUG_MSG
            Serial.print("D::Laser Info Received");
#endif
            g_InputString1.remove(0, (g_InputString1.indexOf('\n'))+1);
          }    
          else if(g_InputString1.indexOf("L_BG_R")==0) //R
          {
            g_bAllOff=false;
            g_bBlinkActivated=false;
            delay(200);
            for(int j=0; j<MAX_LEDS_BG; j++)
            {
                strip_bg.setPixelColor(j, strip_bg.Color(0, 0, 0)); 
            }
            strip_bg.show();
            for(int j=0; j<MAX_LEDS_BG; j++)
            {
                strip_bg.setPixelColor(j, strip_bg.Color(255, 0, 0)); 
            }
            strip_bg.show();
#ifdef DEBUG_MSG
            Serial.print("D::Laser Info Received");
#endif
            g_InputString1.remove(0, (g_InputString1.indexOf('\n'))+1);
          }    
          else if(g_InputString1.indexOf("L_BG_B")==0) //B
          {
            g_bAllOff=false;
            g_bBlinkActivated=false;
            delay(200);
            for(int j=0; j<MAX_LEDS_BG; j++)
            {
                strip_bg.setPixelColor(j, strip_bg.Color(0, 0, 0)); 
            }
            strip_bg.show();
            for(int j=0; j<MAX_LEDS_BG; j++)
            {
                strip_bg.setPixelColor(j, strip_bg.Color(0, 0, 255)); 
            }
            strip_bg.show();
#ifdef DEBUG_MSG
            Serial.print("D::Laser Info Received");
#endif
            g_InputString1.remove(0, (g_InputString1.indexOf('\n'))+1);
          }    
          else if(g_InputString1.indexOf("ABORT_1")==0) //ALL OFF!
          {
            AllOff();
#ifdef DEBUG_MSG
          Serial.print("D:: Received ABORT");
#endif
            g_InputString1.remove(0, (g_InputString1.indexOf('\n'))+1);
          }
          else if(g_InputString1.indexOf("ABORT_R")==0) //ALL OFF BUT RED ON IN BG
          {
            AllOff();
            for(int j=0; j<MAX_LEDS_BG; j++)
            {
              strip_bg.setPixelColor(j, strip_bg.Color(255, 0, 0)); //R
              strip_bg.show(); 
            }
#ifdef DEBUG_MSG
          Serial.print("D:: Received ABORT");
#endif
            g_InputString1.remove(0, (g_InputString1.indexOf('\n'))+1);
          }
          else 
            g_bClearOneCmd1 = true;
      }
    else 
      g_bClearOneCmd1 = true;
    if (g_bClearOneCmd1==true) //cleas till 1st appearance of NL, incl. NL
    {
			if (g_InputString1.indexOf('\n')>=0)  
      {
#ifdef DEBUG_MSG
        Serial.print("D::cleared string:");   Serial.println(g_InputString1);
#endif
        g_InputString1.remove(0, (g_InputString1.indexOf('\n'))+1);
      }
      //TODO: 'else' delete all if NL not received?
			g_bClearOneCmd1=false;	   
    }
    if ((g_InputString1.indexOf('\n')<0)) 
      g_bStringComplete1=false;
    g_bSerLock1 = false;
  }
}


void AllOff()
{
  g_bAllOff=true;
  g_bBlinkActivated=false;
  delay(200);
  for(int j=0; j<MAX_LEDS_ARROW; j++)
  {
    strip_arrow.setPixelColor(j, strip_arrow.Color(0, 0, 0)); //0
    strip_arrow.show(); 
  }
  for(int j=0; j<MAX_LEDS_RING; j++)
  {
    strip_ring.setPixelColor(j, strip_ring.Color(0, 0, 0)); //0
    strip_ring.show(); 
  }
  for(int j=0; j<MAX_LEDS_PROG; j++)
  {
    strip_progress.setPixelColor(j, strip_progress.Color(0, 0, 0)); //0
    strip_progress.show(); 
  }
  for(int j=0; j<MAX_LEDS_BG; j++)
  {
    strip_bg.setPixelColor(j, strip_bg.Color(0, 0, 0)); //0
    strip_bg.show(); 
  }
  delay(200);
}