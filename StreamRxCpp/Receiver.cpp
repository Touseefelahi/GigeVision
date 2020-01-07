#include "Receiver.h"

namespace Receiver
{
	void Bind(unsigned short port)
	{
		sockaddr_in add;
		add.sin_family = AF_INET;
		add.sin_addr.s_addr = htonl(INADDR_ANY);
		add.sin_port = htons(port);

		int ret = bind(sock, reinterpret_cast<SOCKADDR*>(&add), sizeof(add));
		if (ret < 0)
			throw std::system_error(WSAGetLastError(), std::system_category(), "Bind failed");
		int n = width * height * bytesPerPixel * 500;
		if (setsockopt(sock, SOL_SOCKET, SO_RCVBUF, (char*)&n, sizeof(n)) == -1) {
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
		sock = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
		if (sock == INVALID_SOCKET)
		{
			throw std::system_error(WSAGetLastError(), std::system_category(), "Error opening socket");
			return false;
		}
		width = widthIn;
		height = heightIn;

		Bind(port);
		packetLength = 10000;//Max  value
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
			int payloadSize = recvfrom(sock, buffer, packetLength, flags, reinterpret_cast<SOCKADDR*>(&from), &size);
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

			int id1 = (byte)buffer[6];
			int id2 = (byte)buffer[7];

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

	bool Stop()
	{
		isListening = false;
		closesocket(sock);
		shutdown(sock, 2);
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