use anyhow::Result;
use contracts::debug_requires;

use crate::chunk::Chunk;
use crate::chunk::opcode::{FromOpCodeError, OpCode};
use crate::compiler;
use crate::value::Value;

const STACK_MAX: usize = 256;

pub struct VM {
    chunk: Chunk,
    ip: usize,
    stack: Vec<Value>,
}

pub enum InterpretResult {
    Ok,
    CompileError,
    RuntimeError,
}

impl VM {
    fn new(chunk: Chunk) -> VM {
        VM {
            chunk,
            ip: 0,
            stack: Vec::with_capacity(STACK_MAX),
        }
    }

    pub fn interpret(source: String) -> InterpretResult {
        match compiler::compile(source) {
            Err(_) => InterpretResult::CompileError,
            Ok(chunk) => VM::new(chunk).run()
        }
    }

    fn read_byte(&mut self) -> u8 {
        let byte = self.chunk.code_at(self.ip);
        self.ip += 1;
        byte
    }

    fn read_op_code(&mut self) -> Result<OpCode, FromOpCodeError> {
        OpCode::try_from(self.read_byte())
    }

    fn read_constant(&mut self) -> Value {
        let constant_offset = self.read_byte();
        self.chunk.constant_at(constant_offset)
    }

    fn push(&mut self, value: Value) {
        self.stack.push(value)
    }

    #[debug_requires(!self.stack.is_empty())]
    fn pop(&mut self) -> Value {
        self.stack.pop().expect("stack shouldn't be empty")
    }

    fn binary_op<F>(&mut self, f: F)
    where
        F: Fn(Value, Value) -> Value,
    {
        let b = self.pop();
        let a = self.pop();
        self.push(f(a, b));
    }

    fn run(&mut self) -> InterpretResult {
        loop {
            if self.ip >= self.chunk.code_length() {
                return InterpretResult::Ok;
            }

            #[cfg(feature = "debug_trace")]
            {
                self.chunk.disassemble_instruction(self.ip);
                print!("          ");
                for slot in self.stack.iter() {
                    print!("[ {slot} ]");
                }
                println!();
            }

            match self.read_op_code() {
                Ok(op_code) => match op_code {
                    OpCode::Return => {
                        println!("{}", self.stack.pop().unwrap());
                        return InterpretResult::Ok;
                    }
                    OpCode::Constant => {
                        let constant = self.read_constant();
                        self.stack.push(constant);
                    }
                    OpCode::Negate => {
                        let top = self.pop();
                        self.push(-top);
                    }
                    OpCode::Add => self.binary_op(|a, b| a + b),
                    OpCode::Subtract => self.binary_op(|a, b| a - b),
                    OpCode::Multiply => self.binary_op(|a, b| a * b),
                    OpCode::Divide => self.binary_op(|a, b| a / b),
                },
                Err(_) => return InterpretResult::RuntimeError,
            }
        }
    }
}
