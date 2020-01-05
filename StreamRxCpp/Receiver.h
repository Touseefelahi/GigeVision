#pragma once
#include <stdio.h>
#include <conio.h>
#include <winsock2.h>
#include <WS2tcpip.h>
#include <system_error>
#include <string>
#include <iostream>
#include <thread>
#pragma comment(lib, "Ws2_32.lib")
namespace Receiver
{
	SOCKET sock;
	int packetLength;
	char* buffer;
	unsigned char* imageData, * startingAddress;
	sockaddr_in from;
	int flags = 0;
	bool isListening;
	int width, height, bytesPerPixel = 1;
	unsigned long globalCounterInvalid = 0, globalCounterValid = 0, numberOfMissedPackets;

	extern "C" typedef void(__stdcall* ProgressCallback)(int);
	extern "C" __declspec(dllexport) bool Start(long port, unsigned char** imageDataAddress, long width, long height, long packetLengthToSet, ProgressCallback progressCallback);
	extern "C" __declspec(dllexport) bool Stop();
	extern "C" __declspec(dllexport) bool IsFrameReady();
	extern "C" __declspec(dllexport) bool UpdateFrame();
	extern "C" __declspec(dllexport) unsigned long GetCurrentInvalidFrameCounter();
	extern "C" __declspec(dllexport) unsigned long GetCurrentValidFrameCounter();
	extern "C" __declspec(dllexport) void FrameIncoming(ProgressCallback progressCallback);
	void ReceiveStreamContinous();
}
