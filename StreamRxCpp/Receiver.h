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
	SOCKET globalSock;
	int packetLength = 1000;
	char* buffer;
	unsigned char* imageData, * startingAddress, * startingAddressImage;
	sockaddr_in from;
	int flags = 0;
	bool isListening;
	int width = 1920, height = 1080, bytesPerPixel = 1;
	unsigned long globalCounterInvalid = 0, globalCounterValid = 0, numberOfMissedPackets;

	extern "C" typedef void(__stdcall* ProgressCallback)(int);
	extern "C" __declspec(dllexport) bool Start(long port, unsigned char** imageDataAddress, long width, long height, long bytesPerPixel, ProgressCallback frameReady);
	extern "C" __declspec(dllexport) bool GetProcessedFrame(long port, unsigned char** imageDataAddress, ProgressCallback frameReady);
	extern "C" __declspec(dllexport) bool Stop();
	extern "C" __declspec(dllexport) unsigned long GetCurrentInvalidFrameCounter();
	extern "C" __declspec(dllexport) unsigned long GetCurrentValidFrameCounter();
	void BayerGr2RGB(unsigned char* imageDataCopy, unsigned char* startingAddressImage2, ProgressCallback frameReady);
}
