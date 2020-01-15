#include "Receiver.h"
#pragma warning(disable:4996)
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

	void JoinMulticastGroup(const char* group)
	{
		struct ip_mreq mreq;
		mreq.imr_interface.s_addr = htonl(INADDR_ANY);
		mreq.imr_multiaddr.s_addr = inet_addr(group);
		if (setsockopt(globalSock, IPPROTO_IP, IP_ADD_MEMBERSHIP, (char*)&mreq, sizeof(mreq)) < 0)
		{
			throw std::system_error(WSAGetLastError(), std::system_category(), "Multicast Group Join Failed");
		}
		std::cout << "Multicast Group Joined successfully" << std::endl;
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

	bool GetRawFrame(long port, const char* group, unsigned char** imageDataAddress, ProgressCallback frameReady)
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
		if (group != NULL)
		{
			JoinMulticastGroup(group);
		}
		packetLength = 10000;//Max  value
		char* bufferLocal = static_cast<char*>(malloc(8 * packetLength));
		int size = sizeof(from);
		isListening = true;

		int lastPayloadSize = 0;
		bool isValidFrame = false;
		int finalPacketID = 0;
		int payloadSizeNormal = 0;
		unsigned int packetID;
		unsigned char* imageAddressRaw, * startingAddressRaw = NULL;
		int bytesPerpixel2, widthLocal, heightLocal;
		bool informationGathered = false;
		bool isLeaderProcessed = false;
		while (!informationGathered)
		{
			int payloadSize = recvfrom(globalSock, bufferLocal, packetLength, flags, reinterpret_cast<SOCKADDR*>(&from), &size);
			if (payloadSize < 0)
				return false;

			packetID = (unsigned int)(((byte)bufferLocal[6] * 256) + (byte)bufferLocal[7]);
			if (packetID == 0)
			{
				bytesPerpixel2 = (int)((byte)(bufferLocal[21]) / (byte)8);
				widthLocal = (int)(((byte)bufferLocal[26] * 256) + (byte)bufferLocal[27]);
				heightLocal = (int)(((byte)bufferLocal[30] * 256) + (byte)bufferLocal[31]);

				isLeaderProcessed = true;

				*imageDataAddress = static_cast<unsigned char*>(malloc((size_t)(8 * widthLocal * heightLocal * bytesPerpixel2)));
				imageAddressRaw = (*imageDataAddress);
				startingAddressRaw = (*imageDataAddress);

				continue;
			}
			if (packetID == 1 && isLeaderProcessed)
			{
				int totalBytes = widthLocal * heightLocal * bytesPerpixel2;
				lastPayloadSize = totalBytes % (payloadSize - 8);
				finalPacketID = totalBytes / (payloadSize - 8);
				if (lastPayloadSize != 0)finalPacketID += 1;
				payloadSizeNormal = payloadSize;
				if (isLeaderProcessed)informationGathered = true;
			}
		}

		while (isListening)
		{
			int payloadSize = recvfrom(globalSock, bufferLocal, packetLength, flags, reinterpret_cast<SOCKADDR*>(&from), &size);
			if (payloadSize < 0)
				return false;
			else if (bufferLocal[4] == 0x02)//Trailer
			{
				frameReady(globalCounterValid);
				continue;
			}
			else if (bufferLocal[4] == 0x01)//Leader
			{
				continue;
			}
			packetID = (unsigned int)(((byte)bufferLocal[6] * 256) + (byte)bufferLocal[7]);
			bufferLocal += 8;

			if (packetID == finalPacketID)
			{
				imageAddressRaw = startingAddressRaw + ((packetID - 1) * (payloadSizeNormal - 8));
				memcpy(imageAddressRaw, bufferLocal, payloadSize - 8);
			}
			else
			{
				imageAddressRaw = startingAddressRaw + ((packetID - 1) * (payloadSize - 8));
				memcpy(imageAddressRaw, bufferLocal, payloadSize - 8);
			}
			bufferLocal -= 8;
		}

		return true;
	}

	void GetRawPixels(int finalPacketID, int payloadSizeNormal,
		unsigned char* imageAddressRaw, unsigned char* startingAddressRaw, ProgressCallback frameReady)
	{
		char* bufferLocal = static_cast<char*>(malloc(8 * packetLength));
		unsigned int packetID;
		int size = sizeof(from);
		while (isListening)
		{
			int payloadSize = recvfrom(globalSock, bufferLocal, packetLength, flags, reinterpret_cast<SOCKADDR*>(&from), &size);
			if (payloadSize < 0)
				return;
			else if (bufferLocal[4] == 0x02)//Trailer
			{
				frameReady(globalCounterValid);
				continue;
			}
			else if (bufferLocal[4] == 0x01)//Leader
			{
				continue;
			}
			packetID = (unsigned int)(((byte)bufferLocal[6] * 256) + (byte)bufferLocal[7]);
			bufferLocal += 8;

			if (packetID == finalPacketID)
			{
				imageAddressRaw = startingAddressRaw + ((packetID - 1) * (payloadSizeNormal - 8));
				memcpy(imageAddressRaw, bufferLocal, payloadSize - 8);
			}
			else
			{
				imageAddressRaw = startingAddressRaw + ((packetID - 1) * (payloadSize - 8));
				memcpy(imageAddressRaw, bufferLocal, payloadSize - 8);
			}
			bufferLocal -= 8;
		}
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
		char* bufferLocal;
		bufferLocal = static_cast<char*>(malloc(8 * packetLength));
		unsigned char* ringBuffer = NULL, * ringBufferStart = NULL;
		int size = sizeof(from);
		isListening = true;

		int counter = 0;
		int lastPayloadSize = 0;
		bool isJustStarted = true;
		bool isValidFrame = false;
		int finalPacketID = 0;
		int payloadSizeNormal = 0;
		unsigned int packetID;
		unsigned char* startingAddressImage2, * startingAddressImageNew, * imageDataCopy = NULL, * imageData2 = NULL, * startingAddressRaw = NULL;
		int bytesPerpixel2, width2, height2;
		bool informationGathered = false;
		bool isLeaderProcessed = false;
		bool isBayerPattern = false;
		startingAddressImageNew = NULL;
		while (!informationGathered)
		{
			int payloadSize = recvfrom(globalSock, bufferLocal, packetLength, flags, reinterpret_cast<SOCKADDR*>(&from), &size);
			if (payloadSize < 0)
				return false;

			packetID = (unsigned int)(((byte)bufferLocal[6] * 256) + (byte)bufferLocal[7]);
			if (packetID == 0)
			{
				bytesPerpixel2 = (int)((byte)(bufferLocal[21]) / (byte)8);
				width2 = (int)(((byte)bufferLocal[26] * 256) + (byte)bufferLocal[27]);
				height2 = (int)(((byte)bufferLocal[30] * 256) + (byte)bufferLocal[31]);
				if ((byte)bufferLocal[23] > 7 && (byte)bufferLocal[23] < 0x0c)
				{
					isBayerPattern = true;
				}
				isLeaderProcessed = true;
				if (isBayerPattern)
				{
					*imageDataAddress = static_cast<unsigned char*>(malloc((size_t)(8 * width2 * height2 * 3)));
					startingAddressImageNew = *imageDataAddress;
					startingAddressImage2 = startingAddressImageNew;
					imageData2 = static_cast<unsigned char*>(malloc((size_t)(8 * width2 * height2)));
					imageDataCopy = static_cast<unsigned char*>(malloc((size_t)(8 * width2 * height2)));
					startingAddressRaw = imageData2;
				}
				else
				{
					*imageDataAddress = static_cast<unsigned char*>(malloc((size_t)(8 * width2 * height2 * bytesPerpixel2)));
					imageData2 = static_cast<unsigned char*>(malloc((size_t)(8 * width2 * height2 * bytesPerpixel2)));
					imageData2 = (*imageDataAddress);
					startingAddressRaw = imageData2;
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

		if (isBayerPattern)
		{
			if (payloadSizeNormal >= 3 * width2)
			{
				ringBuffer = static_cast<unsigned char*>(malloc(8 * payloadSizeNormal));
			}
			else if (payloadSizeNormal < width2)
			{
				ringBuffer = static_cast<unsigned char*>(malloc(8 * 3 * width2));
			}
			else
			{
				ringBuffer = static_cast<unsigned char*>(malloc(8 * 3 * payloadSizeNormal));
			}
			ringBufferStart = ringBuffer;
		}

		int totalPixelsReceived = 0;
		int rowProcessed = 0;
		unsigned int pixelProcessedCount = 0;
		startingAddressImage2 = startingAddressImageNew;
		std::cout << "StartAddressB" << &startingAddressImage2 << std::endl;
		int ringCount = 0;
		while (isListening)
		{
			int payloadSize = recvfrom(globalSock, bufferLocal, packetLength, flags, reinterpret_cast<SOCKADDR*>(&from), &size);
			if (payloadSize < 0)
				return false;
			else if (bufferLocal[4] == 0x02)//Trailer
			{
				frameReady(globalCounterValid);
				totalPixelsReceived = 0;
				startingAddressImage2 = startingAddressImageNew;
				rowProcessed = 0;
				pixelProcessedCount = 0;
				continue;
			}
			else if (bufferLocal[4] == 0x01)//Leader
			{
				totalPixelsReceived = 0;
				startingAddressImage2 = startingAddressImageNew;
				rowProcessed = 0;
				pixelProcessedCount = 0;
				continue;
			}
			packetID = (unsigned int)(((byte)bufferLocal[6] * 256) + (byte)bufferLocal[7]);
			bufferLocal += 8;
			if (!isBayerPattern)
			{
				if (packetID == finalPacketID)
				{
					imageData2 = startingAddressRaw + ((packetID - 1) * (payloadSizeNormal - 8));
					memcpy(imageData2, bufferLocal, payloadSize - 8);
				}
				else
				{
					imageData2 = startingAddressRaw + ((packetID - 1) * (payloadSize - 8));
					memcpy(imageData2, bufferLocal, payloadSize - 8);
				}
			}
			else	//BayerGr8
			{
				totalPixelsReceived += (payloadSize - 8);
				int midRow = 0, upperRow = 0, lowerRow = 0;
				switch (ringCount)
				{
				case 0:
					ringBuffer = ringBufferStart;
					memcpy(ringBuffer, bufferLocal, payloadSize - 8);
					ringCount = 1;
					upperRow = width2;
					midRow = 2 * width2;
					lowerRow = 0;
					break;
				case 1:
					ringBuffer = ringBufferStart + (payloadSize - 8);
					memcpy(ringBuffer, bufferLocal, payloadSize - 8);
					ringCount = 2;
					lowerRow = width2;
					upperRow = 2 * width2;
					midRow = 0;
					break;
				case 2:
					ringBuffer = ringBufferStart + (2 * (payloadSize - 8));
					memcpy(ringBuffer, bufferLocal, payloadSize - 8);
					ringCount = 0;
					midRow = width2;
					lowerRow = 2 * width2;
					upperRow = 0;
					break;
				}

				ringBuffer = ringBufferStart;
				unsigned char pixelValueR = 0, pixelValueG = 0, pixelValueB = 0;
				for (int row = rowProcessed; row < totalPixelsReceived / width2; row++)
				{
					for (int col = 0; col < width2; col++)
					{
						if (row > 0 && col > 0 && row < height2 - 1 && col < width2 - 1)
						{
							if (row % 2 == 0 && col % 2 == 0)
							{
								pixelValueG = ringBuffer[midRow + col];
								pixelValueR = (ringBuffer[midRow + col - 1] + ringBuffer[midRow + col + 1]) / 2;
								pixelValueB = (ringBuffer[upperRow + col] + ringBuffer[lowerRow + col]) / 2;
							}
							else if (row % 2 == 1 && col % 2 == 0)
							{
								pixelValueB = ringBuffer[midRow + col];
								pixelValueR = (ringBuffer[upperRow + col - 1] + ringBuffer[upperRow + col - 1]) / 2;
								pixelValueG = (ringBuffer[upperRow + col] + ringBuffer[lowerRow + col]) / 2;
							}
							else if (row % 2 == 0 && col % 2 == 1)
							{
								pixelValueR = ringBuffer[midRow + col];
								pixelValueG = (ringBuffer[upperRow + col] + ringBuffer[lowerRow + col]) / 2;
								pixelValueB = (ringBuffer[upperRow + col - 1] + ringBuffer[upperRow + col - 1]) / 2;
							}
							else
							{
								pixelValueG = ringBuffer[midRow + col];
								pixelValueR = (ringBuffer[upperRow + col] + ringBuffer[lowerRow + col]) / 2;
								pixelValueB = (ringBuffer[midRow + col - 1] + ringBuffer[midRow + col + 1]) / 2;
							}
						}
						*startingAddressImage2++ = pixelValueR;
						*startingAddressImage2++ = pixelValueG;
						*startingAddressImage2++ = pixelValueB;
					}
					rowProcessed++;
				}
			}
			bufferLocal -= 8;
		}
		return true;
	}

	void BayerGr2RGB(unsigned char* imageDataCopy, unsigned char* startingAddressImage2, ProgressCallback frameReady)
	{
		unsigned char pixelValueR = 0, pixelValueG = 0, pixelValueB = 0;
		for (int row = 0; row < height; row++)
		{
			for (int col = 0; col < width; col++)
			{
				if (row > 0 && col > 0 && row < height - 1 && col < width - 1)
				{
					if (row % 2 == 0 && col % 2 == 0)
					{
						pixelValueG = imageDataCopy[row * width + col];
						pixelValueR = (imageDataCopy[(row * width) + col - 1] + imageDataCopy[(row * width) + col + 1]) / 2;
						pixelValueB = (imageDataCopy[((row - 1) * width) + col] + imageDataCopy[((row + 1) * width) + col]) / 2;
					}
					else if (row % 2 == 1 && col % 2 == 0)
					{
						pixelValueB = imageDataCopy[row * width + col];
						pixelValueR = (imageDataCopy[((row - 1) * width) + col - 1] + imageDataCopy[((row - 1) * width) + col - 1]) / 2;
						pixelValueG = (imageDataCopy[((row - 1) * width) + col] + imageDataCopy[((row + 1) * width) + col]) / 2;
					}
					else if (row % 2 == 0 && col % 2 == 1)
					{
						pixelValueR = imageDataCopy[row * width + col];
						pixelValueG = (imageDataCopy[((row - 1) * width) + col] + imageDataCopy[((row + 1) * width) + col]) / 2;
						pixelValueB = (imageDataCopy[((row - 1) * width) + col - 1] + imageDataCopy[((row - 1) * width) + col - 1]) / 2;
					}
					else
					{
						pixelValueG = imageDataCopy[row * width + col];
						pixelValueR = (imageDataCopy[((row - 1) * width) + col] + imageDataCopy[((row + 1) * width) + col]) / 2;
						pixelValueB = (imageDataCopy[(row * width) + col - 1] + imageDataCopy[(row * width) + col + 1]) / 2;
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
		frameReady(globalCounterValid);
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