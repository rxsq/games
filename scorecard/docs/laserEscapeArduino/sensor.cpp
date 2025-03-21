#include <Arduino.h>

#define UART_BAUD 115200  
#define MAX_LASERS 48
#define HEADER 0xCB  // Start byte 0xCA for first controller and 0xCB for second controller
#define FOOTER 0x0A  // End byte

const int cSensor[MAX_LASERS] = { A0, A1, A2, A3, A4, A5, 
                                  A6, A7, A8, A9, A10, A11,
                                  A12, A13, A14, A15, 20, 21,
                                  47, 48, 49, 50, 51, 52,
                                  41, 42, 43, 44, 45, 46,
                                  35, 36, 37, 38, 39, 40,
                                  29, 30, 31, 32, 33, 34,
                                  23, 24, 25, 26, 27, 28 };

void setup() {
    Serial.begin(UART_BAUD); // Debug output (USB)
    Serial1.begin(UART_BAUD); // Communication with Zigbee

    for (int i = 0; i < MAX_LASERS; i++) {
        pinMode(cSensor[i], INPUT);
    }
}

void loop() {
    uint8_t dataPacket[8]; // 1-byte header + 6 sensor bytes + 1-byte footer
    dataPacket[0] = HEADER; // First byte is always 0xCB

    // Read sensors and pack into bytes
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

    dataPacket[7] = FOOTER; // Last byte is always 0x0A

    Serial1.write(dataPacket, 8); // Send via Zigbee
    Serial.write(dataPacket, 8);
    // Serial.print("Sent: ");
    // for (int i = 0; i < 8; i++) {
    //     Serial.print(dataPacket[i], HEX);
    //     Serial.print(" ");
    // }
    // Serial.println();

    delay(200); // Adjust as needed
}
