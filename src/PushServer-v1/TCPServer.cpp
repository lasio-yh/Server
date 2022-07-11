#include "stdafx.h"
#include "TCPServer.h"
#include <chrono>
#include <ctime>    

int create_server(){
    WSADATA wsadata;
    SOCKET s;
    SOCKADDR_IN server_address;
    memset(&sock_array, 0, sizeof(sock_array));
    total_socket_count = 0;
    if (WSAStartup(MAKEWORD(2, 2), &wsadata) != 0) {
		std::cout<<"WSAStartup Error."<<std::endl;
        return -1;
    }
    if ((s = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP)) < 0) {
		std::cout<<"Socket Error."<<std::endl;
        return -1;
    }
    memset(&server_address, 0, sizeof(server_address));
    server_address.sin_family = AF_INET;
    server_address.sin_addr.s_addr = htonl(INADDR_ANY);
    server_address.sin_port = htons(port_number);
    if (bind(s, (struct sockaddr*)&server_address, sizeof(server_address)) < 0) {
		std::cout<<"Bind Error."<<std::endl;
        return -2;
    }
    if (listen(s, SOMAXCONN) < 0) {
		std::cout<<"Listen Error."<<std::endl;
        return -3;
    }
    return s;
}
 
int close_server(){
    for (int i = 1; i < total_socket_count; i++) {
        closesocket(sock_array[i].s);
        WSACloseEvent(sock_array[i].ev);
    }
    return 0;
}
 
unsigned int WINAPI init_server(void* param) {
    SOCKET  server_socket;
    WSANETWORKEVENTS ev;
    int index;
    WSAEVENT handle_array[client_count + 1];
    server_socket = create_server();
    if (server_socket < 0) {
		std::cout<<" Server Fail Init."<<std::endl;
        exit(0);
    }
    else{
		std::cout<<"Server Success Init."<<" PortNumber ["<<port_number<<"]"<<std::endl;
        HANDLE event = WSACreateEvent();
        sock_array[total_socket_count].ev = event;
        sock_array[total_socket_count].s = server_socket;
        strcpy_s(sock_array[total_socket_count].ipaddr, "0.0.0.0");
        WSAEventSelect(server_socket, event, FD_ACCEPT);
        total_socket_count++;
        while (true){
            memset(&handle_array, 0, sizeof(handle_array));
            for (int i = 0; i < total_socket_count; i++)
                handle_array[i] = sock_array[i].ev;
            index = WSAWaitForMultipleEvents(total_socket_count, handle_array, false, INFINITE, false);
            if ((index != WSA_WAIT_FAILED) && (index != WSA_WAIT_TIMEOUT)) {
                WSAEnumNetworkEvents(sock_array[index].s, sock_array[index].ev, &ev);
                if (ev.lNetworkEvents == FD_ACCEPT) add_client(index);
                else if (ev.lNetworkEvents == FD_READ) read_client(index);
                else if (ev.lNetworkEvents == FD_CLOSE) remove_client(index);
            }
        }
        closesocket(server_socket);
    }
    WSACleanup();
    _endthreadex(0);
    return 0;
}
 
int add_client(int index){
    SOCKADDR_IN addr;
    int len = 0;
    SOCKET accept_sock;
	PACKET_INFO packet;
    if (total_socket_count == FD_SETSIZE) return 1;
    else { 
        len = sizeof(addr);
        memset(&addr, 0, sizeof(addr));
        accept_sock = accept(sock_array[0].s, (SOCKADDR*)&addr, &len);
        HANDLE event = WSACreateEvent();
        sock_array[total_socket_count].ev = event;
        sock_array[total_socket_count].s = accept_sock;
        strcpy_s(sock_array[total_socket_count].ipaddr, inet_ntoa(addr.sin_addr));
        WSAEventSelect(accept_sock, event, FD_READ | FD_CLOSE);
		std::cout<<"Connect Client "<<inet_ntoa(addr.sin_addr)<<" "<<total_socket_count<<"/"<<FD_SETSIZE<<std::endl;
        total_socket_count++;
		memset(&packet, 0, sizeof(PACKET_INFO));
		packet.command = 0x01;
		packet.type = 0x01;
		packet.id = 0x01;
        notify_client(packet);
    }
    return 0;
}
 
int read_client(int index){
    unsigned int tid;
    HANDLE mainthread = (HANDLE)_beginthreadex(NULL, 0, recv, (void*)index, 0, &tid);
    WaitForSingleObject(mainthread, INFINITE);
    CloseHandle(mainthread);
    return 0;
}
 
void remove_client(int index){
	PACKET_INFO packet;
    char remove_ip[MAXBYTE];
    strcpy_s(remove_ip, get_client_ip(index));
    closesocket(sock_array[index].s);
    WSACloseEvent(sock_array[index].ev);
    total_socket_count--;
    sock_array[index].s = sock_array[total_socket_count].s;
    sock_array[index].ev = sock_array[total_socket_count].ev;
    strcpy_s(sock_array[index].ipaddr, sock_array[total_socket_count].ipaddr);
	std::cout<<"Disconnect Client "<<remove_ip<<" "<<total_socket_count-1<<"/"<<FD_SETSIZE<<std::endl;
	memset(&packet, 0, sizeof(PACKET_INFO));
	packet.command = 0x02;
	packet.type = 0x01;
	packet.id = 0x01;
    notify_client(packet);
}

unsigned int WINAPI recv(void* param){
    int index = (int)param;
    char message[sizeof(PACKET_INFO)];
	PACKET_INFO packet_request;
	PACKET_INFO packet_response;
    SOCKADDR_IN client_address;
    int recv_len = 0,  addr_len  = 0;
    memset(&client_address, 0, sizeof(client_address));
    if ((recv_len = recv(sock_array[index].s, message, sizeof(PACKET_INFO), 0)) > 0){
        addr_len = sizeof(client_address);
        getpeername(sock_array[index].s, (SOCKADDR*)&client_address, &addr_len);
		memcpy(&packet_request, &message, sizeof(PACKET_INFO));
		show_display_request(packet_request);

		//Rule Process
		memset(&packet_response, 0, sizeof(PACKET_INFO));
		packet_response.id = 0x01;
		packet_response.command = 0x02;
		packet_response.type = 0x00;
		packet_response.length = sizeof(PACKET_INFO);
		notify_client(packet_response);
    }
    _endthreadex(0);
    return 0;
}
 
char* get_client_ip(int index){
    static char			ipaddress[256];
    int					addr_len;
    struct sockaddr_in  sock;
    addr_len = sizeof(sock);
    if (getpeername(sock_array[index].s, (struct sockaddr*)& sock, &addr_len) < 0) return NULL;
    strcpy_s(ipaddress, inet_ntoa(sock.sin_addr));
    return ipaddress;
 }
 
int notify_client(PACKET_INFO packet){
	char message[sizeof(PACKET_INFO)];
	memcpy(&message, &packet, sizeof(PACKET_INFO));
	for (int i = 1; i < total_socket_count; i++){
		packet.id = i;
	    send(sock_array[i].s, message, sizeof(PACKET_INFO), 0);
		show_display_response(packet);
	}
    return 0;
}

void show_display_request(PACKET_INFO packet){
	std::cout<<"Request Id "<<static_cast<int>(packet.id)<<std::ends;
	std::cout<<" Command "<<static_cast<int>(packet.command)<<std::ends;
	std::cout<<" Type "<<static_cast<int>(packet.type)<<std::ends;
	std::cout<<" Length "<<static_cast<int>(packet.length)<<std::ends;
	std::cout<<" Data "<<packet.data<<std::endl;
}

void show_display_response(PACKET_INFO packet){
		
	std::cout<<" Response Id "<<static_cast<int>(packet.id)<<std::ends;
	std::cout<<" Command "<<static_cast<int>(packet.command)<<std::ends;
	std::cout<<" Type "<<static_cast<int>(packet.type)<<std::ends;
	std::cout<<" Length "<<static_cast<int>(packet.length)<<std::ends;
	std::cout<<" Data "<<packet.data<<std::endl;
}

void show_now_time()
{
	auto time = std::chrono::system_clock::now();
	std::time_t now_time = std::chrono::system_clock::to_time_t(time);
	std::cout << "starting server at "<< std::ctime(&now_time)<<std::endl;
}

int main(int argc, char* argv[]){
	show_now_time();
    unsigned int tid;
    char message[MAXBYTE] = "";
    HANDLE mainthread;
    if(argv[1] != NULL) port_number = atoi(argv[1]);
    mainthread = (HANDLE)_beginthreadex(NULL, 0, init_server, (void*)0, 0, &tid);
    if (mainthread){
        while (1){
            gets_s(message, MAXBYTE);
            if (strcmp(message, "/x") == 0) break;
        }
        create_server();
        WSACleanup();
        CloseHandle(mainthread);
    }
    return 0;
}