use std::{fs, io, process::exit};

use clap::Parser;

use crate::vm::VM;

mod chunk;
mod compiler;
mod disassembler;
mod scanner;
mod utils;
mod value;
mod vm;

#[derive(clap::Parser)]
struct Cli {
    /// File to run
    file: Option<String>,
}

fn main() {
    let cli = Cli::parse();

    let vm = VM::new();

    if let Some(file_path) = cli.file {
        run_file(vm, &file_path);
    }

    repl(vm)
}

fn repl(vm: VM) {
    let mut line = String::new();
    let stdin = io::stdin();

    loop {
        print!("> ");
        stdin.read_line(&mut line).unwrap();
        println!();

        interpret(&line);
    }
}

fn run_file(vm: VM, file_path: &str) -> ! {
    let source = match fs::read_to_string(file_path) {
        Ok(source) => source,
        Err(err) => {
            eprintln!("Could not open file {file_path}: {err}");
            exit(74);
        }
    };
    let result = interpret(&source);

    match result {
        vm::InterpretResult::Ok => exit(0),
        vm::InterpretResult::CompileError => exit(65),
        vm::InterpretResult::RuntimeError => exit(70),
    }
}

fn interpret(source: &str) -> vm::InterpretResult {
    compiler::compile(source);
    vm::InterpretResult::Ok
}
