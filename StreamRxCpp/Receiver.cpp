#include "Receiver.h"

namespace Receiver
{
	void Bind(unsigned short port)
	{
		sockaddr_in add;
		add.sin_family = AF_INET;
		add.sin_addr.s_addr = htonl(INADDR_ANY);
		add.sin_port = htons(port);

		int ret = bind(globalSock, reinterpret_cast<SOCKADDR*>(&add), sizeof(add));
		if (ret < 0)
			throw std::system_error(WSAGetLastError(), std::system_category(), "Bind failed");
		int sizeBuffer = width * height * 30;
		if (setsockopt(globalSock, SOL_SOCKET, SO_RCVBUF, (char*)&sizeBuffer, sizeof(sizeBuffer)) == -1) {
			throw std::system_error(WSAGetLastError(), std::system_category(), "Rx Buffer size set failed");
		}
	}

	bool Start(long port, unsigned char** imageDataAddress, long widthIn, long heightIn, long bytesPerPixel, ProgressCallback frameReady)
	{
		WSADATA wsaData;
		int res = WSAStartup(MAKEWORD(2, 0), &wsaData);
		if (res == 0) {
			std::cout << "WSAStartup successful" << std::endl;
		}
		else {
			std::cout << "Error WSAStartup" << std::endl;
			return -201;
		}
		globalSock = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		if (globalSock == INVALID_SOCKET)
		{
			throw std::system_error(WSAGetLastError(), std::system_category(), "Error opening socket");
			return false;
		}
		width = widthIn;
		height = heightIn;

		Bind(port);
		buffer = static_cast<char*>(malloc(8 * packetLength));
		*imageDataAddress = static_cast<unsigned char*>(malloc(8 * widthIn * heightIn * bytesPerPixel));
		imageData = (*imageDataAddress);
		startingAddress = (*imageDataAddress);
		int size = sizeof(from);
		isListening = true;
		unsigned int packetIDarray[1000];

		int counter = 0;
		int lastPayloadSize = 0;
		imageData = (startingAddress);
		bool isJustStarted = true;
		bool isValidFrame = false;
		int finalPacketID = 0;
		int payloadSizeNormal = 0;
		unsigned long valid, invalid;
		invalid = globalCounterInvalid;
		valid = globalCounterValid;
		unsigned int packetID;
		while (isListening)
		{
			int payloadSize = recvfrom(globalSock, buffer, packetLength, flags, reinterpret_cast<SOCKADDR*>(&from), &size);
			if (payloadSize < 0)
				return false;
			if (payloadSize < 44)
			{
				if ((counter - 1) != packetIDarray[counter - 1])
				{
					globalCounterInvalid++;
					invalid = globalCounterInvalid;
				}
				else
				{
					frameReady(globalCounterValid);
					globalCounterValid++;
					valid = globalCounterValid;
				}
			}
			if (payloadSize == 44)
			{
				counter = 0;
				imageData = (startingAddress);
			}

			packetID = (unsigned int)(((byte)buffer[6] * 256) + (byte)buffer[7]);
			packetIDarray[counter++] = packetID;
			if (packetID == 0) continue;
			if (packetID == 1)
			{
				int totalBytes = width * height * bytesPerPixel;
				lastPayloadSize = totalBytes % (payloadSize - 8);
				finalPacketID = totalBytes / (payloadSize - 8);
				if (lastPayloadSize != 0)finalPacketID += 1;
				payloadSizeNormal = payloadSize;
			}
			buffer += 8;
			if (packetID == finalPacketID)
			{
				imageData += ((packetID - 1) * (payloadSizeNormal - 8));
				memcpy(imageData, buffer, payloadSize - 8);
				imageData -= ((packetID - 1) * (payloadSizeNormal - 8));
			}
			else
			{
				imageData += ((packetID - 1) * (payloadSize - 8));
				memcpy(imageData, buffer, payloadSize - 8);
				imageData -= ((packetID - 1) * (payloadSize - 8));
			}
			buffer -= 8;
		}
		return true;
	}

	bool GetProcessedFrame(long port, unsigned char** imageDataAddress, ProgressCallback frameReady)
	{
		WSADATA wsaData;
		int res = WSAStartup(MAKEWORD(2, 0), &wsaData);
		if (res == 0) {
			std::cout << "WSAStartup successful" << std::endl;
		}
		else {
			std::cout << "Error WSAStartup" << std::endl;
			return -201;
		}
		globalSock = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		if (globalSock == INVALID_SOCKET)
		{
			throw std::system_error(WSAGetLastError(), std::system_category(), "Error opening socket");
			return false;
		}

		Bind(port);
		packetLength = 10000;//Max  value
		buffer = static_cast<char*>(malloc(8 * packetLength));

		int size = sizeof(from);
		isListening = true;

		int counter = 0;
		int lastPayloadSize = 0;
		bool isJustStarted = true;
		bool isValidFrame = false;
		int finalPacketID = 0;
		int payloadSizeNormal = 0;
		unsigned int packetID;
		unsigned char* startingAddressImage2;
		unsigned int bytesPerpixel2, width2, height2;
		bool informationGathered = false;
		bool isLeaderProcessed = false;
		bool isBayerPattern = false;
		while (!informationGathered)
		{
			int payloadSize = recvfrom(globalSock, buffer, packetLength, flags, reinterpret_cast<SOCKADDR*>(&from), &size);
			if (payloadSize < 0)
				return false;

			packetID = (unsigned int)(((byte)buffer[6] * 256) + (byte)buffer[7]);
			if (packetID == 0)
			{
				bytesPerpixel2 = (unsigned int)((byte)(buffer[21]) / (byte)8);
				width2 = (unsigned int)(((byte)buffer[26] * 256) + (byte)buffer[27]);
				height2 = (unsigned int)(((byte)buffer[30] * 256) + (byte)buffer[31]);
				if ((byte)buffer[23] > 7 && (byte)buffer[23] < 0x0c)
				{
					isBayerPattern = true;
				}
				isLeaderProcessed = true;
				if (isBayerPattern)
				{
					*imageDataAddress = static_cast<unsigned char*>(malloc((size_t)(8 * width2 * height2 * 3)));
					startingAddressImage = *imageDataAddress;
					startingAddressImage2 = startingAddressImage;
					imageData = static_cast<unsigned char*>(malloc((size_t)(8 * width2 * height2 * bytesPerpixel2)));
					startingAddress = imageData;
				}
				else
				{
					*imageDataAddress = static_cast<unsigned char*>(malloc((size_t)(8 * width2 * height2 * bytesPerpixel2)));
					imageData = (*imageDataAddress);
					startingAddress = imageData;
				}
				continue;
			}
			if (packetID == 1 && isLeaderProcessed)
			{
				int totalBytes = width2 * height2 * bytesPerpixel2;
				lastPayloadSize = totalBytes % (payloadSize - 8);
				finalPacketID = totalBytes / (payloadSize - 8);
				if (lastPayloadSize != 0)finalPacketID += 1;
				payloadSizeNormal = payloadSize;
				if (isLeaderProcessed)informationGathered = true;
			}
		}

		while (isListening)
		{
			int payloadSize = recvfrom(globalSock, buffer, packetLength, flags, reinterpret_cast<SOCKADDR*>(&from), &size);
			if (payloadSize < 0)
				return false;
			else if (payloadSize < 44)
			{
				if (isBayerPattern)
				{
					imageData = (startingAddress);
					startingAddressImage2 = startingAddressImage;
					unsigned char pixelValueR = 0, pixelValueG = 0, pixelValueB = 0;
					for (int row = 0; row < height2; row++)
					{
						for (int col = 0; col < width2; col++)
						{
							if (row > 0 && col > 0 && row < height2 - 1 && col < width2 - 1)
							{
								if (row % 2 == 0 && col % 2 == 0)
								{
									pixelValueG = imageData[row * width2 + col];
									pixelValueR = (imageData[(row * width2) + col - 1] + imageData[(row * width2) + col + 1]) / 2;
									pixelValueB = (imageData[((row - 1) * width2) + col] + imageData[((row + 1) * width2) + col]) / 2;
								}
								else if (row % 2 == 1 && col % 2 == 0)
								{
									pixelValueB = imageData[row * width2 + col];
									pixelValueR = (imageData[((row - 1) * width2) + col - 1] + imageData[((row - 1) * width2) + col - 1]) / 2;
									pixelValueG = (imageData[((row - 1) * width2) + col] + imageData[((row + 1) * width2) + col]) / 2;
								}
								else if (row % 2 == 0 && col % 2 == 1)
								{
									pixelValueR = imageData[row * width2 + col];
									pixelValueG = (imageData[((row - 1) * width2) + col] + imageData[((row + 1) * width2) + col]) / 2;
									pixelValueB = (imageData[((row - 1) * width2) + col - 1] + imageData[((row - 1) * width2) + col - 1]) / 2;
								}
								else
								{
									pixelValueG = imageData[row * width2 + col];
									pixelValueR = (imageData[((row - 1) * width2) + col] + imageData[((row + 1) * width2) + col]) / 2;
									pixelValueB = (imageData[(row * width2) + col - 1] + imageData[(row * width2) + col + 1]) / 2;
								}
							}
							else
							{
							}
							*startingAddressImage2++ = pixelValueR;
							*startingAddressImage2++ = pixelValueG;
							*startingAddressImage2++ = pixelValueB;
						}
					}
					imageData = (startingAddress);
					frameReady(globalCounterValid);
				}
				else
				{
					frameReady(globalCounterValid);
				}
			}
			else if (payloadSize == 44)
			{
				imageData = (startingAddress);
			}

			packetID = (unsigned int)(((byte)buffer[6] * 256) + (byte)buffer[7]);
			if (packetID == 0) continue;

			buffer += 8;
			if (packetID == finalPacketID)
			{
				imageData = startingAddress + ((packetID - 1) * (payloadSizeNormal - 8));
				memcpy(imageData, buffer, payloadSize - 8);
			}
			else
			{
				imageData = startingAddress + ((packetID - 1) * (payloadSize - 8));
				memcpy(imageData, buffer, payloadSize - 8);
			}
			buffer -= 8;
		}
		return true;
	}

	bool Stop()
	{
		isListening = false;
		closesocket(globalSock);
		shutdown(globalSock, 2);
		return true;
	}

	unsigned long GetCurrentInvalidFrameCounter()
	{
		return globalCounterInvalid;
	}

	unsigned long GetCurrentValidFrameCounter()
	{
		return globalCounterValid;
	}
}