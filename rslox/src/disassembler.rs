use crate::{
    chunk::{Chunk, OpCode},
    value::print_value,
};

pub fn disassemble_chunk(chunk: &Chunk, name: &str) {
    println!("=== {name} ===");

    let mut offset = 0;
    while offset < chunk.code.len() {
        offset = disassemble_instruction(chunk, offset);
    }
}

pub fn disassemble_instruction(chunk: &Chunk, offset: usize) -> usize {
    print!("{offset:04} ");
    if offset > 0 && chunk.lines[offset] == chunk.lines[offset - 1] {
        print!("   | ");
    } else {
        print!("{:>4} ", chunk.lines[offset]);
    }

    if let Ok(instruction) = OpCode::try_from(chunk.code[offset]) {
        use OpCode::*;

        match instruction {
            Return | Negate | Add | Subtract | Multiply | Divide => {
                simple_instruction(instruction, offset)
            }
            Constant | ConstantLong => constant_instruction(instruction, chunk, offset),
        }
    } else {
        println!("Unknown opcode {}", chunk.code[offset]);
        offset + 1
    }
}

fn simple_instruction(opcode: OpCode, offset: usize) -> usize {
    println!("{opcode:?}");
    offset + 1
}

fn constant_instruction(opcode: OpCode, chunk: &Chunk, offset: usize) -> usize {
    match opcode {
        OpCode::Constant => {
            let constant = chunk.code[offset + 1];
            print!("{opcode:-16?} {constant:04} '");
            print_value(chunk.constants[constant as usize]);
            println!("'");

            offset + 2
        }
        OpCode::ConstantLong => {
            let constant = (chunk.code[offset + 1] as usize) << 16
                | (chunk.code[offset + 2] as usize) << 8
                | (chunk.code[offset + 3] as usize);
            print!("{opcode:-16?} {constant:04} '");
            print_value(chunk.constants[constant]);
            println!("'");

            offset + 4
        }
        _ => unreachable!("should only call constant_instruction on Constant or ConstantLong"),
    }
}
