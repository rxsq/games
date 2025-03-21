#include <Arduino.h>

#define UART_BAUD 115200  
#define MAX_LASERS 48

const byte HEADER_BYTE = 0xFA; // 0xFA for first controller and 0xFB for second controller
const byte FOOTER_BYTE = 0x0A;

const int laserPins[MAX_LASERS] = { 
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
  Serial.begin(UART_BAUD); // Debug output (USB)
    Serial1.begin(UART_BAUD); // Communication with Zigbee module

    for (int i = 0; i < MAX_LASERS; i++) {
        pinMode(laserPins[i], OUTPUT);
        digitalWrite(laserPins[i], HIGH); // Start with lasers ON
    }
}

void loop() {
    byte buffer[8];  // 1 header + 6 laser data bytes + 1 footer

    if (Serial1.available()) {
        int bytesRead = Serial1.readBytesUntil(FOOTER_BYTE, buffer, sizeof(buffer));
        Serial.print("Received: ");
        for (int i = 0; i < bytesRead; i++) {
            Serial.print(buffer[i], HEX);
            Serial.print(" ");
        }
        Serial.println(bytesRead, DEC);
        if (bytesRead == 7 && buffer[0] == HEADER_BYTE) {  
            Serial.println("Processing data");
            updateLasers(buffer + 1);  // Process laser data (skip header)
        }
    }
}

void updateLasers(byte *laserData) {
    for (int i = 0; i < MAX_LASERS; i++) {
        int byteIndex = i / 8;
        int bitIndex = i % 8;

        // Check if the bit for this laser is set
        bool laserOn = (laserData[byteIndex] & (1 << bitIndex)) != 0;
        digitalWrite(laserPins[i], laserOn ? HIGH : LOW);
    }
}
