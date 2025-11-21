use std::{env, fs};
use std::io::{stdin, stdout, Write};
use std::process::exit;

use crate::vm::{InterpretResult, VM};

mod chunk;
mod compiler;
mod scanner;
mod value;
mod vm;

fn repl() {
    let mut line = String::new();
    loop {
        print!("> ");
        stdout().flush();

        line.clear();
        let bytes_read = stdin().read_line(&mut line).expect("can read from stdin");

        if bytes_read == 0 {
            println!();
            break;
        }

        VM::interpret(line.clone());
    }
}

fn runFile(file_path: &str) {
    match fs::read_to_string(file_path) {
        Ok(source) => {
            let result = VM::interpret(source);

            match result {
                InterpretResult::CompileError => exit(65),
                InterpretResult::RuntimeError => exit(70),
                _ => {}
            };
        }
        Err(err) => {
            eprintln!("Cannot open source file on path '{file_path}': {err}");
            exit(74);
        }
    }
}

fn main() {
    let args = env::args().collect::<Vec<_>>();

    match args.as_slice() {
        [_executable] => repl(),
        [_executable, file] => runFile(file),
        _ => {
            eprintln!("Usage: rslox [path]");
            exit(64);
        }
    }
}
