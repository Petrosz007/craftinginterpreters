use contracts::{debug_ensures, debug_requires};

use crate::scanner::TokenType::And;

#[derive(Debug, Eq, PartialEq, Copy, Clone)]
pub enum TokenType {
    // Single-character tokens.
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    Comma,
    Dot,
    Minus,
    Plus,
    Semicolon,
    Slash,
    Star,
    // One or two character tokens.
    Bang,
    BangEqual,
    Equal,
    EqualEqual,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,
    // Literals.
    Identifier,
    String,
    Number,
    // Keywords.
    And,
    Class,
    Else,
    False,
    For,
    Fun,
    If,
    Nil,
    Or,
    Print,
    Return,
    Super,
    This,
    True,
    Var,
    While,

    Error,
    Eof,
}

#[derive(Clone)]
pub enum Token<'a> {
    Ok {
        token_type: TokenType,
        token: &'a str,
        line: usize,
    },
    Error {
        message: String,
        line: usize,
    },
}

pub struct Scanner {
    source: String,
    source_chars: Vec<char>,
    start_index: usize,
    current_index: usize,
    line: usize,
}

impl Scanner {
    #[debug_requires(self.start_index < self.source.len())]
    fn start(&self) -> char {
        self.source_chars[self.start_index]
    }

    #[debug_requires(self.current_index < self.source.len())]
    fn current(&self) -> char {
        self.source_chars[self.current_index]
    }

    fn is_at_end(&self) -> bool {
        self.current_index == self.source.len() - 1
    }

    pub(crate) fn advance(&mut self) -> char {
        self.current_index += 1;
        self.source_chars[self.current_index - 1]
    }

    fn matches(&mut self, expected: char) -> bool {
        if self.is_at_end() {
            return false;
        }
        if self.current() != expected {
            return false;
        }
        self.current_index += 1;
        true
    }

    fn peek(&mut self) -> Option<char> {
        if self.is_at_end() {
            None
        } else {
            Some(self.current())
        }
    }

    fn peek_next(&mut self) -> Option<char> {
        if self.is_at_end() {
            None
        } else if self.current_index + 1 == self.source_chars.len() {
            None
        } else {
            Some(self.source_chars[self.current_index + 1])
        }
    }

    fn skip_whitespace(&mut self) {
        loop {
            match self.peek() {
                Some(' ' | '\r' | '\t') => {
                    self.advance();
                }
                Some('\n') => {
                    self.line += 1;
                    self.advance();
                }
                Some('/') => {
                    if self.peek() == Some('/') {
                        // A comment goes until the end of the line.
                        while self.peek_next() != Some('\n') && !self.is_at_end() {
                            self.advance();
                        }
                    } else {
                        return;
                    }
                }
                _ => {
                    return;
                }
            }
        }
    }

    fn check_keyword(&self, start: usize, rest: &'static str, token_type: TokenType) -> TokenType {
        if self.current_index - self.start_index == start + rest.len()
            && &self.source[self.start_index + start..self.start_index + rest.len() + 1] == rest
        {
            token_type
        } else {
            TokenType::Identifier
        }
    }

    fn identifier_type(&self) -> TokenType {
        use TokenType::*;

        match self.source_chars[self.start_index] {
            'a' => return self.check_keyword(1, "nd", And),
            'c' => return self.check_keyword(1, "lass", Class),
            'e' => return self.check_keyword(1, "lse", Else),
            'f' if self.current_index - self.start_index > 1 => {
                match self.source_chars[self.start_index + 1] {
                    'a' => return self.check_keyword(2, "lse", False),
                    'o' => return self.check_keyword(2, "r", For),
                    'u' => return self.check_keyword(2, "n", Fun),
                    _ => {}
                }
            }
            'i' => return self.check_keyword(1, "f", If),
            'n' => return self.check_keyword(1, "il", Nil),
            'o' => return self.check_keyword(1, "r", Or),
            'p' => return self.check_keyword(1, "rint", Print),
            'r' => return self.check_keyword(1, "eturn", Return),
            's' => return self.check_keyword(1, "uper", Super),
            't' if self.current_index - self.start_index > 1 => {
                match self.source_chars[self.start_index + 1] {
                    'h' => return self.check_keyword(2, "is", This),
                    'r' => return self.check_keyword(2, "ue", True),
                    _ => {}
                }
            }
            'v' => return self.check_keyword(1, "ar", Var),
            'w' => return self.check_keyword(1, "hile", While),
            _ => {}
        }

        Identifier
    }

    fn identifier(&mut self) -> Token {
        while self.peek().map(|c| c.is_alphabetic() || c.is_ascii_digit()) == Some(true) {
            self.advance();
        }

        self.make_token(self.identifier_type())
    }

    fn number(&mut self) -> Token {
        while self.peek().map(|c| c.is_ascii_digit()) == Some(true) {
            self.advance();
        }

        if self.peek() == Some('.') && self.peek_next().map(|c| c.is_ascii_digit()) == Some(true) {
            // Consume the '.'
            self.advance();

            while self.peek().map(|c| c.is_ascii_digit()) == Some(true) {
                self.advance();
            }
        }

        self.make_token(TokenType::Number)
    }

    fn string(&mut self) -> Token {
        while self.peek() != Some('"') && !self.is_at_end() {
            if self.peek() == Some('\n') {
                self.line += 1;
            }

            self.advance();
        }

        if self.is_at_end() {
            return self.error_token("Unterminated string.");
        }

        // The closing quote
        self.advance();
        self.make_token(TokenType::String)
    }

    fn make_token(&self, token_type: TokenType) -> Token {
        Token::Ok {
            token_type,
            token: &self.source[self.start_index..self.current_index],
            line: self.line,
        }
    }

    fn error_token(&self, message: impl Into<String>) -> Token {
        Token::Error {
            message: message.into(),
            line: self.line,
        }
    }

    pub fn scan_token(&mut self) -> Token {
        use TokenType::*;
        self.skip_whitespace();

        self.start_index = self.current_index;

        if self.is_at_end() {
            return self.make_token(TokenType::Eof);
        }

        match self.advance() {
            c if c.is_alphabetic() => return self.identifier(),
            c if c.is_ascii_digit() => return self.number(),
            '(' => return self.make_token(LeftParen),
            ')' => return self.make_token(RightParen),
            '{' => return self.make_token(LeftBrace),
            '}' => return self.make_token(RightBrace),
            ';' => return self.make_token(Semicolon),
            ',' => return self.make_token(Comma),
            '.' => return self.make_token(Dot),
            '-' => return self.make_token(Minus),
            '+' => return self.make_token(Plus),
            '/' => return self.make_token(Slash),
            '*' => return self.make_token(Star),
            '!' => {
                let token_type = if self.matches('=') { BangEqual } else { Bang };
                return self.make_token(token_type);
            }
            '=' => {
                let token_type = if self.matches('=') { EqualEqual } else { Equal };
                return self.make_token(token_type);
            }
            '<' => {
                let token_type = if self.matches('=') { LessEqual } else { Less };
                return self.make_token(token_type);
            }
            '>' => {
                let token_type = if self.matches('=') {
                    GreaterEqual
                } else {
                    Greater
                };
                return self.make_token(token_type);
            }
            '"' => return self.string(),
            _ => {}
        }

        return self.error_token("Unexpected character");
    }
}

impl Scanner {
    #[debug_ensures(ret.source.len() == ret.source_chars.len())]
    pub fn new(source: String) -> Self {
        Scanner {
            source_chars: source.chars().collect(),
            source,
            start_index: 0,
            current_index: 0,
            line: 1,
        }
    }
}
