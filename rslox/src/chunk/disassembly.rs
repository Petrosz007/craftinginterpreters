use contracts::debug_requires;

use crate::chunk::Chunk;
use crate::chunk::opcode::OpCode;
use crate::chunk::opcode::OpCode::*;

impl Chunk {
    pub fn disassemble(&self, name: &str) {
        println!("== {name} ==");

        let mut offset = 0;
        while offset < self.code.len() {
            offset = self.disassemble_instruction(offset);
        }
    }

    #[debug_requires(offset < self.code.len())]
    #[debug_requires(self.code.len() == self.lines.len())]
    pub fn disassemble_instruction(&self, offset: usize) -> usize {
        print!("{offset:04} ");
        if offset > 0 && self.lines[offset] == self.lines[offset - 1] {
            print!("   | ")
        } else {
            print!("{:4} ", self.lines[offset])
        }

        match OpCode::try_from(self.code[offset]) {
            Ok(op_code) => match op_code {
                x @ (Return | Negate | Add | Subtract | Multiply | Divide) => {
                    self.simple_instruction(x, offset)
                }
                x @ Constant => self.constant_instruction(x, offset),
            },
            Err(err) => {
                println!("{err}");
                offset + 1
            }
        }
    }

    fn simple_instruction(&self, op_code: OpCode, offset: usize) -> usize {
        println!("{op_code:?}");
        offset + 1
    }

    #[debug_requires(self.code.len() > offset + 1)]
    #[debug_requires(self.constants.len() >= self.code[offset + 1] as usize)]
    fn constant_instruction(&self, op_code: OpCode, offset: usize) -> usize {
        let constant_id = self.code[offset + 1];
        println!(
            "{op_code:16?} {constant_id:4} '{}'",
            self.constants[constant_id as usize]
        );
        offset + 2
    }
}
