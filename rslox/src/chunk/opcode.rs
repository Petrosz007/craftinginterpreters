use thiserror::Error;

use OpCode::*;

#[repr(u8)]
#[derive(Debug, Copy, Clone)]
pub enum OpCode {
    Return = 0,
    Constant = 1,
    Negate = 2,
    Add = 3,
    Subtract = 4,
    Multiply = 5,
    Divide = 6,
}

impl From<OpCode> for u8 {
    fn from(value: OpCode) -> Self {
        // Rust docs say that this is safe: https://doc.rust-lang.org/reference/items/enumerations.html#pointer-casting
        unsafe { *(&value as *const OpCode as *const u8) }
    }
}

#[derive(Error, Debug)]
pub enum FromOpCodeError {
    #[error("Unknown OpCode '{0}'")]
    Unknown(u8),
}

impl TryFrom<u8> for OpCode {
    type Error = FromOpCodeError;

    fn try_from(value: u8) -> anyhow::Result<Self, Self::Error> {
        match value {
            0 => Ok(Return),
            1 => Ok(Constant),
            2 => Ok(Negate),
            3 => Ok(Add),
            4 => Ok(Subtract),
            5 => Ok(Multiply),
            6 => Ok(Divide),
            // TODO: Use https://doc.rust-lang.org/std/mem/fn.variant_count.html once stable for additional compile time guarantees
            x => Err(FromOpCodeError::Unknown(x)),
        }
    }
}
