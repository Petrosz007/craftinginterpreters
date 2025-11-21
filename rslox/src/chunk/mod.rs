use contracts::debug_requires;

use crate::value::Value;

pub mod disassembly;
pub mod opcode;

pub struct Chunk {
    code: Vec<u8>,
    constants: Vec<Value>,
    lines: Vec<usize>,
}

impl Chunk {
    pub fn new() -> Chunk {
        Chunk {
            code: Vec::new(),
            constants: Vec::new(),
            lines: Vec::new(),
        }
    }

    pub fn write(&mut self, byte: impl Into<u8>, line: usize) {
        self.code.push(byte.into());
        self.lines.push(line);
    }

    #[debug_requires(self.constants.len() < (u8::MAX as usize))]
    pub fn add_constant(&mut self, constant: impl Into<Value>) -> u8 {
        self.constants.push(constant.into());
        (self.constants.len() - 1) as u8
    }

    pub fn code_length(&self) -> usize {
        self.code.len()
    } 
    
    pub fn constant_length(&self) -> usize {
        self.code.len()
    }

    #[debug_requires(offset < self.code.len())]
    pub fn code_at(&self, offset: usize) -> u8 {
        self.code[offset]
    }

    #[debug_requires((offset as usize) < self.constants.len())]
    pub fn constant_at(&self, offset: u8) -> Value {
        self.constants[offset as usize]
    }
}
