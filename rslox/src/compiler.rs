use anyhow::Result;

use crate::chunk::Chunk;
use crate::scanner::{Scanner, Token};

struct Parser<'a> {
    scanner: Scanner,
    current: Token<'a>,
    previous: Token<'a>,
}

impl<'a> Parser<'a> {
    fn advance(&'a mut self) {
        self.previous = self.current.clone();

        loop {
            let x = self.scanner.scan_token();
            self.current = x;

            match self.current {
                Token::Ok {
                    token_type,
                    token,
                    line: token_line,
                } => {}
                Token::Error { message, line } => {
                    eprintln!("Parse error on line {line}: {message}");
                }
            }
        }
    }
}

pub fn compile(source: String) -> Result<Chunk> {
    let mut scanner = Scanner::new(source);

    scanner.advance();
    todo!();
    // expression();
    // consume(TokenType::Eof, "Expected end of expression");
}
