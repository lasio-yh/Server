#define _WINSOCK_DEPRECATED_NO_WARNINGS
#define _CRT_SECURE_NO_WARNINGS

#pragma comment(lib, "ws2_32.lib")
#pragma warning(disable : 4996)

typedef struct  packet_info{
	WORD				id;					// 식별자.
	char				command;			// 명령어. (C : 0x00, R : 0x01, U : 0x02, D : 0x03)
	char				type;				// 수행 결과. (T : 0x00, F : 0x01, 0x02 ...)
	long				length;				// 패킷 크기.
	BYTE				data[248];			// 데이터. (json string)
}PACKET_INFO; 

typedef struct sock_info{
    SOCKET s;
    HANDLE ev;
    char ipaddr[50];
}SOCK_INFO;
 
int         port_number = 9999;
const int	client_count = 10;
SOCK_INFO   sock_array[client_count + 1];
int         total_socket_count = 0;

int create_server();
int close_server();
unsigned int WINAPI init_server(void* param);
unsigned int WINAPI recv(void* param);
int add_client(int index);
int read_client(int index);
void remove_client(int index);
int notify_client(PACKET_INFO packet);
char* get_client_ip(int index);
void show_display_request(PACKET_INFO packet);
void show_display_response(PACKET_INFO packet);
void show_now_time();