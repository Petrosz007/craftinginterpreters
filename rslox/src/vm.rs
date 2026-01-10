use std::{ops::Range, pin::Pin, ptr};

use crate::{
    chunk::{Chunk, OP},
    compiler,
    disassembler::disassemble_instruction,
    value::{Value, print_value},
};

// const STACK_MAX: usize = 256;
const STACK_MAX: usize = 256;

pub struct VM {
    chunk: Pin<Box<Chunk>>,
    ip: *const u8,
    ip_range: Range<*const u8>,

    // TODO: I think I need to store the underlying struct here for the pointers to work
    #[allow(unused)]
    stack: Pin<Box<[Value; STACK_MAX]>>,
    stack_top: *mut Value,
    stack_ptr_range: Range<*mut Value>,
}

pub enum InterpretResult {
    Ok,
    #[allow(unused)]
    CompileError,
    #[allow(unused)]
    RuntimeError,
}

impl VM {
    pub fn new() -> VM {
        let mut vm = VM {
            chunk: Box::pin(Chunk::new()),
            ip: ptr::null(),
            ip_range: Range::default(),
            stack: Box::pin([0.0; STACK_MAX]),
            stack_top: ptr::null_mut(),
            stack_ptr_range: Range::default(),
        };

        vm.ip = vm.chunk.code.as_ptr();
        vm.ip_range = vm.chunk.code.as_ptr_range();
        vm.stack_top = vm.stack.as_mut_ptr();
        vm.stack_ptr_range = vm.stack.as_mut_ptr_range();

        vm
    }

    pub fn interpret(&mut self, source: &str) -> InterpretResult {
        let chunk = match compiler::Compiler::compile(source) {
            Ok(chunk) => chunk,
            Err(_) => return InterpretResult::CompileError,
        };

        dbg!(self.stack.as_mut_ptr_range(), &self.stack_ptr_range);

        self.chunk = Box::pin(chunk);
        self.ip = self.chunk.code.as_ptr();
        self.ip_range = self.chunk.code.as_ptr_range();

        dbg!(self.stack.as_mut_ptr_range(), &self.stack_ptr_range);

        self.run()
    }

    fn run(&mut self) -> InterpretResult {
        loop {
            #[cfg(feature = "debug_trace_execution")]
            {
                print!("          ");
                let mut ptr = self.stack_ptr_range.start;
                // SAFETY: We are in range on the stack, because we start from the stack start ptr, and end with the stack_top, which is also in range of the stack
                unsafe {
                    while ptr < self.stack_top {
                        print!("[");
                        print_value(*ptr);
                        print!("]");
                        ptr = ptr.add(1);
                    }
                }
                println!();

                disassemble_instruction(&self.chunk, unsafe {
                    self.ip.offset_from_unsigned(self.ip_range.start)
                });
            }

            match self.read_byte() {
                OP::RETURN => {
                    print_value(self.pop());
                    println!();
                    return InterpretResult::Ok;
                }
                OP::CONSTANT => {
                    let value = self.read_constant();
                    self.push(value);
                }
                OP::CONSTANT_LONG => {
                    let value = self.read_constant_long();
                    self.push(value);
                }
                OP::NEGATE => {
                    // TODO(optimisation): We could mutate the value in place through the stack pointer
                    let value = -self.pop();
                    self.push(value);
                }
                OP::ADD => {
                    // TODO(optimisation): We could mutate the value in place through the stack pointer
                    let b = self.pop();
                    let a = self.pop();
                    self.push(a + b);
                }
                OP::SUBTRACT => {
                    // TODO(optimisation): We could mutate the value in place through the stack pointer
                    let b = self.pop();
                    let a = self.pop();
                    self.push(a - b);
                }
                OP::MULTIPLY => {
                    // TODO(optimisation): We could mutate the value in place through the stack pointer
                    let b = self.pop();
                    let a = self.pop();
                    self.push(a * b);
                }
                OP::DIVIDE => {
                    // TODO(optimisation): We could mutate the value in place through the stack pointer
                    let b = self.pop();
                    let a = self.pop();
                    self.push(a / b);
                }
                unknown_opcode => panic!("Unknown opcode: {unknown_opcode:04}"),
            }
        }
    }

    fn read_byte(&mut self) -> u8 {
        // TODO(safety): What guarantees that we are in range of the chunk.code slice?
        let byte = unsafe { self.ip.read() };
        // TODO(safety): What guarantees that we remain in range of the chunk.code slice?
        self.ip = unsafe { self.ip.add(1) };
        byte
    }

    fn read_constant(&mut self) -> Value {
        let index = self.read_byte() as usize;
        self.chunk.constants[index]
    }

    fn read_constant_long(&mut self) -> Value {
        let index0 = self.read_byte() as usize;
        let index1 = self.read_byte() as usize;
        let index2 = self.read_byte() as usize;
        self.chunk.constants[index0 << 16 | index1 << 8 | index2]
    }

    fn reset_stack(&mut self) {
        self.stack = Box::pin([0.0; STACK_MAX]);
        self.stack_top = self.stack.as_mut_ptr();
        self.stack_ptr_range = self.stack.as_mut_ptr_range();
    }

    fn push(&mut self, value: Value) {
        // SAFETY: The stack pointer always points within the range of the stack
        // TODO(safety): What if it points to the end, so the first memory value after the end?
        unsafe { *self.stack_top = value };

        // TODO(safety): Check if we would index out of the stack?
        unsafe {
            self.stack_top = self.stack_top.add(1);
        };
    }

    fn pop(&mut self) -> Value {
        // TODO(safety): What if we have no values on the stack? This would index out
        unsafe {
            self.stack_top = self.stack_top.sub(1);
        };

        // SAFETY: We have checked that the pointer is in range of the stack
        unsafe { *self.stack_top }
    }
}
