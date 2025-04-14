#include <Arduino.h>

#define UART_BAUD 115200  
#define MAX_LASERS 48
#define HEADER 0xCA  // Start byte 0xCB for this controller
#define FOOTER 0x0A  // End byte
#define CMD_HEADER 0xCC

bool SEND = true; // Make SEND a variable instead of a macro

const int cSensor[MAX_LASERS] = {
  A0, A1, A2, A3, A4, A5, 
  A6, A7, A8, A9, A10, A11,
  A12, A13, A14, A15, 20, 21,
  47, 48, 49, 50, 51, 52,
  41, 42, 43, 44, 45, 46,
  35, 36, 37, 38, 39, 40,
  29, 30, 31, 32, 33, 34,
  23, 24, 25, 26, 27, 28
};

void setup() {
  Serial.begin(UART_BAUD);     // USB Debug
  Serial1.begin(UART_BAUD);    // Communication with external controller (e.g., Zigbee)
  
  for (int i = 0; i < MAX_LASERS; i++) {
    pinMode(cSensor[i], INPUT);
  }
}

void checkIncomingCommand() {
  while (Serial1.available() >= 3) {
    if (Serial1.peek() == CMD_HEADER) {
      uint8_t buffer[3];
      Serial1.readBytes(buffer, 3);

      if (buffer[2] == FOOTER) {
        if (buffer[1] == 0x00) {
          SEND = false;
          Serial.println("SEND set to FALSE");
        } else if (buffer[1] == 0xFF) {
          SEND = true;
          Serial.println("SEND set to TRUE");
        }
      }
    } else {
      // Discard byte if it doesn't match expected header
      Serial1.read();
    }
  }
}

void sendSensorData() {
  uint8_t dataPacket[8]; // 1-byte header + 6 sensor bytes + 1-byte footer
  dataPacket[0] = HEADER;

  for (int i = 0; i < 6; i++) {
    uint8_t packedByte = 0;
    for (int bit = 0; bit < 8; bit++) {
      int sensorIndex = (i * 8) + bit;
      if (sensorIndex < MAX_LASERS) {
        packedByte |= (digitalRead(cSensor[sensorIndex]) == LOW) << bit;
      }
    }
    dataPacket[i + 1] = packedByte;
  }

  dataPacket[7] = FOOTER;

  Serial1.write(dataPacket, 8);
  Serial.write(dataPacket, 8);
}

void loop() {
  checkIncomingCommand();

  if (SEND) {
    sendSensorData();
  }

  delay(200); // Adjust as needed
}
