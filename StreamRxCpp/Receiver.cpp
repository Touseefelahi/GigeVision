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
		int n = width * height * 500;
		if (setsockopt(sock, SOL_SOCKET, SO_RCVBUF, (char*)&n, sizeof(n)) == -1) {
			throw std::system_error(WSAGetLastError(), std::system_category(), "Rx Buffer size set failed");
		}
	}

	bool Start(long port, unsigned char** imageDataAddress, long widthIn, long heightIn, long packetLengthToSet, ProgressCallback progressCallback)
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
		packetLength = packetLengthToSet;
		buffer = static_cast<char*>(malloc(8 * packetLength));
		*imageDataAddress = static_cast<unsigned char*>(malloc(8 * widthIn * heightIn));
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
		while (isListening)
		{
			int payloadSize = recvfrom(sock, buffer, packetLength, flags, reinterpret_cast<SOCKADDR*>(&from), &size);
			if (payloadSize < 0)
				throw std::system_error(WSAGetLastError(), std::system_category(), "recvfrom failed");
			if (payloadSize < 44)
			{
				if (counter - 1 != packetIDarray[counter - 1])
				{
					globalCounterInvalid++;
					invalid = globalCounterInvalid;
				}
				else
				{
					progressCallback(globalCounterValid);
					globalCounterValid++;
					valid = globalCounterValid;
				}
			}
			if (payloadSize == 44)
			{
				counter = 0;
				imageData = (startingAddress);
			}

			unsigned int packetID = (unsigned int)((buffer[6] * 256) + (unsigned char)buffer[7]);
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

	bool isFramePushRequired = false;
	void ReceiveStreamContinous()
	{
		int size = sizeof(from);
		while (isListening)
		{
			int ret = recvfrom(sock, buffer, packetLength, flags, reinterpret_cast<SOCKADDR*>(&from), &size);
			if (ret < 0)
				throw std::system_error(WSAGetLastError(), std::system_category(), "recvfrom failed");
			if (ret < 44)
			{
				isFramePushRequired = true;
			}
			else if (ret < 100)
			{
				imageData = startingAddress;
				continue;
			}

			int packetID = buffer[6] * 256 + buffer[7];
			if (packetID == 0) continue;
			buffer += 8;
			memcpy(imageData, buffer, ret - 8);
			imageData += (ret - 8);
			buffer -= 8;
		}
	}

	bool Stop()
	{
		isListening = false;
		closesocket(sock);
		return true;
	}

	bool IsFrameReady()
	{
		while (isListening)
		{
			if (isFramePushRequired)
			{
				return true;
			}
			else
			{
				Sleep(5);
			}
		}
	}
	unsigned long GetCurrentInvalidFrameCounter()
	{
		return globalCounterInvalid;
	}
	unsigned long GetCurrentValidFrameCounter()
	{
		return globalCounterValid;
	}

	void FrameIncoming(ProgressCallback progressCallback)
	{
		int counter = 0;

		for (; counter <= 100; counter++)
		{
			// do the work...

			if (progressCallback)
			{
				// send progress update
				progressCallback(counter);
			}
		}
	}

	bool UpdateFrame()
	{
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
		while (isListening)
		{
			int payloadSize = recvfrom(sock, buffer, packetLength, flags, reinterpret_cast<SOCKADDR*>(&from), &size);
			if (payloadSize < 0)
				throw std::system_error(WSAGetLastError(), std::system_category(), "recvfrom failed");
			if (payloadSize < 44)
			{
				if (counter - 1 != packetIDarray[counter - 1])
				{
					globalCounterInvalid++;
					invalid = globalCounterInvalid;
				}
				else
				{
					globalCounterValid++;
					valid = globalCounterValid;
				}
				if (isValidFrame)
				{
					imageData = (startingAddress);
					return true;
					continue;
				}
				else
				{
					counter = 0;
					imageData = (startingAddress);
					continue;
				}
			}

			unsigned int packetID = (unsigned int)((buffer[6] * 256) + (unsigned char)buffer[7]);
			packetIDarray[counter++] = packetID;
			if (packetID == 0) continue;
			if (packetID == 1)
			{
				isValidFrame = true;
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
}