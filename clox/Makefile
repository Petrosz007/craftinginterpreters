files = main.c chunk.c memory.c debug.c value.c vm.c compiler.c scanner.c

flags = -Wall -Wextra -pedantic
std = -std=c99

build:
	gcc ${std} ${flags} ${files} -o main.out

clean:
	rm main.out
