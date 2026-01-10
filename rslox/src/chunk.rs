use serde::{Deserialize, Serialize};

use crate::value::Value;

#[allow(non_snake_case)]
pub mod OP {
    pub const RETURN: u8 = 0;
    pub const CONSTANT: u8 = 1;
    pub const CONSTANT_LONG: u8 = 2;
    pub const NEGATE: u8 = 3;
    pub const ADD: u8 = 4;
    pub const SUBTRACT: u8 = 5;
    pub const MULTIPLY: u8 = 6;
    pub const DIVIDE: u8 = 7;
}

#[repr(u8)]
#[derive(Debug, Clone, Copy)]
pub enum OpCode {
    Return = OP::RETURN,
    Constant = OP::CONSTANT,
    ConstantLong = OP::CONSTANT_LONG,
    Negate = OP::NEGATE,
    Add = OP::ADD,
    Subtract = OP::SUBTRACT,
    Multiply = OP::MULTIPLY,
    Divide = OP::DIVIDE,
    // NOTE: Don't forget to update try_from's implementation
}

impl From<OpCode> for u8 {
    fn from(value: OpCode) -> Self {
        value as u8
    }
}

impl TryFrom<u8> for OpCode {
    type Error = ();

    fn try_from(value: u8) -> Result<Self, Self::Error> {
        const MIN_OPCODE: OpCode = OpCode::Return;
        const MAX_OPCODE: OpCode = OpCode::Divide;

        match value {
            x if x >= MIN_OPCODE as u8 && x <= MAX_OPCODE as u8 => {
                Ok(unsafe { std::mem::transmute::<u8, OpCode>(x) }) // TODO: This is unsafe, but easier (and faster?) than explicitly matching
            }
            _ => Err(()),
        }
    }
}

#[derive(Serialize, Deserialize)]
pub struct Chunk {
    pub code: Vec<u8>,
    pub lines: Vec<usize>,
    pub constants: Vec<Value>,
}

impl Chunk {
    pub fn new() -> Chunk {
        Chunk {
            code: Vec::new(),
            lines: Vec::new(),
            constants: Vec::new(),
        }
    }

    pub fn write(&mut self, byte: u8, line: usize) {
        // Both arrays must be in sync, they must push to the same index
        self.code.push(byte);
        self.lines.push(line); // TODO: Implement run-length encoding for lines
    }

    pub fn write_constant(&mut self, value: Value, line: usize) {
        if self.constants.len() <= u8::MAX as usize {
            self.constants.push(value);
            self.write(OpCode::Constant.into(), line);
            self.write((self.constants.len() - 1) as u8, line);

            return;
        }

        const MAX_CONSTANTS: usize = 0x00FF_FFFF;
        if self.constants.len() <= MAX_CONSTANTS {
            self.constants.push(value);

            let idx = self.constants.len() - 1;
            self.write(OpCode::ConstantLong.into(), line);
            self.write(((idx & 0x00FF_0000) >> 16) as u8, line);
            self.write(((idx & 0x0000_FF00) >> 8) as u8, line);
            self.write((idx & 0x0000_00FF) as u8, line);

            return;
        }

        panic!(
            "Trying to add more than {} constants to a chunk",
            MAX_CONSTANTS
        )
    }
}
