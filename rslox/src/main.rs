use crate::{
    chunk::{Chunk, OpCode},
    disassembler::disassemble_chunk,
    vm::VM,
};

mod chunk;
mod disassembler;
mod value;
mod vm;

fn main() {
    let mut vm = VM::new();

    let mut chunk = Chunk::new();
    chunk.write_constant(1.2, 123);
    chunk.write_constant(3.4, 123);
    chunk.write(OpCode::Add.into(), 123);
    chunk.write_constant(5.6, 123);
    chunk.write(OpCode::Divide.into(), 123);
    chunk.write(OpCode::Negate.into(), 123);
    chunk.write(OpCode::Return.into(), 123);

    disassemble_chunk(&chunk, "test chunk");

    vm.interpret(chunk);
}
