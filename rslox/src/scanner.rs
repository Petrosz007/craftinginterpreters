use crate::utils::Peeknextable;
use crate::utils::UtilsIterator;
use std::str::Chars;

pub struct Scanner<'a> {
    #[allow(unused)] // TODO: I think I need this for the lifecycle of the source_iter
    source: String,
    source_iter: Peeknextable<Chars<'a>>,
    current_str: String,
    start: usize,
    current: usize,
    line: usize,
}

#[derive(PartialEq, Eq, Debug, Clone)]
pub struct Token {
    pub typ: TokenType,
    pub str: String,
    pub line: usize,
}

#[derive(PartialEq, Eq, Debug, Copy, Clone)]
pub enum TokenType {
    // Single-character tokens.
    /// (
    LeftParen,
    /// )
    RightParen,
    /// {
    LeftBrace,
    /// }
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

impl Scanner<'_> {
    pub fn new(source: &str) -> Scanner<'_> {
        Scanner {
            source: source.to_owned(),
            source_iter: source.chars().peeknextable(),
            current_str: String::new(),
            start: 0,
            current: 0,
            line: 1,
        }
    }

    pub fn scan_token(&mut self) -> Token {
        self.skip_whitespace();

        self.start = self.current;

        if self.is_at_end() {
            return self.make_token(TokenType::Eof);
        }

        let c = self.advance();
        match c {
            '(' => return self.make_token(TokenType::LeftParen),
            ')' => return self.make_token(TokenType::RightParen),
            '{' => return self.make_token(TokenType::LeftBrace),
            '}' => return self.make_token(TokenType::RightBrace),
            ';' => return self.make_token(TokenType::Semicolon),
            ',' => return self.make_token(TokenType::Comma),
            '.' => return self.make_token(TokenType::Dot),
            '-' => return self.make_token(TokenType::Minus),
            '+' => return self.make_token(TokenType::Plus),
            '/' => return self.make_token(TokenType::Slash),
            '*' => return self.make_token(TokenType::Star),
            '!' => {
                let typ = if self.matches('=') {
                    TokenType::BangEqual
                } else {
                    TokenType::Bang
                };
                return self.make_token(typ);
            }
            '=' => {
                let typ = if self.matches('=') {
                    TokenType::EqualEqual
                } else {
                    TokenType::Equal
                };
                return self.make_token(typ);
            }
            '<' => {
                let typ = if self.matches('=') {
                    TokenType::LessEqual
                } else {
                    TokenType::Less
                };
                return self.make_token(typ);
            }
            '>' => {
                let typ = if self.matches('=') {
                    TokenType::GreaterEqual
                } else {
                    TokenType::Greater
                };
                return self.make_token(typ);
            }
            '"' => return self.string(),
            c if is_alpha(c) => return self.identifier(),
            c if c.is_ascii_digit() => return self.number(),
            _ => {}
        }

        self.error_token("Unexpected character.")
    }

    fn is_at_end(&mut self) -> bool {
        self.source_iter.peek().is_none()
    }

    fn advance(&mut self) -> char {
        let c = self.source_iter.next().unwrap();
        self.current_str.push(c);
        self.current += c.len_utf8();
        c
    }

    fn matches(&mut self, expected: char) -> bool {
        let Some(next_char) = self.source_iter.peek() else {
            return false;
        };

        if *next_char != expected {
            return false;
        }

        self.advance();
        true
    }

    fn make_token(&mut self, typ: TokenType) -> Token {
        let str = self.current_str.clone();
        self.current_str = String::new();

        Token {
            typ,
            str,
            line: self.line,
        }
    }

    fn error_token(&mut self, message: &str) -> Token {
        self.current_str = String::new();

        Token {
            typ: TokenType::Error,
            str: message.to_owned(),
            line: self.line,
        }
    }

    fn skip_whitespace(&mut self) {
        loop {
            let peek = self.source_iter.peek();
            match peek {
                None => break,
                Some('\n') => {
                    self.line += 1;
                    self.advance();
                }
                Some(c) if c.is_whitespace() => {
                    self.advance();
                }
                Some('/') => {
                    let peek_next = self.source_iter.peek_next();
                    if peek_next == Some(&'/') {
                        // If we peek `//` then read until the end of the line, and then continue the whitespace removal loop
                        while let Some(c) = self.source_iter.next()
                            && c != '\n'
                        {}
                        continue;
                    } else {
                        break;
                    }
                }
                _ => break,
            }
        }

        // All the self.advance()-es accumulated the whitespace in the current_str, we need to clear that
        self.current_str = String::new();
    }

    fn string(&mut self) -> Token {
        while let Some(c) = self.source_iter.peek()
            && *c != '"'
        {
            if *c == '\n' {
                self.line += 1;
            }

            self.advance();
        }

        if self.is_at_end() {
            return self.error_token("Unterminated string");
        }

        self.advance(); // Closing quote
        self.make_token(TokenType::String)
    }

    fn identifier(&mut self) -> Token {
        while let Some(c) = self.source_iter.peek()
            && c.is_ascii_alphanumeric()
        {
            self.advance();
        }

        self.make_token(self.identifier_type())
    }

    fn identifier_type(&self) -> TokenType {
        let mut current = self.current_str.chars();
        match current
            .next()
            .expect("identifier_type to be called after at least a char has been read")
        {
            'a' => check_keyword(&mut current, "nd", TokenType::And),
            'c' => check_keyword(&mut current, "lass", TokenType::Class),
            'e' => check_keyword(&mut current, "lse", TokenType::Else),
            'f' => match current.next() {
                Some('a') => check_keyword(&mut current, "lse", TokenType::False),
                Some('o') => check_keyword(&mut current, "r", TokenType::For),
                Some('u') => check_keyword(&mut current, "n", TokenType::Fun),
                _ => TokenType::Identifier,
            },
            'i' => check_keyword(&mut current, "f", TokenType::If),
            'n' => check_keyword(&mut current, "il", TokenType::Nil),
            'o' => check_keyword(&mut current, "r", TokenType::Or),
            'p' => check_keyword(&mut current, "rint", TokenType::Print),
            'r' => check_keyword(&mut current, "eturn", TokenType::Return),
            's' => check_keyword(&mut current, "uper", TokenType::Super),
            't' => match current.next() {
                Some('h') => check_keyword(&mut current, "is", TokenType::This),
                Some('r') => check_keyword(&mut current, "ue", TokenType::True),
                _ => TokenType::Identifier,
            },
            'v' => check_keyword(&mut current, "ar", TokenType::Var),
            'w' => check_keyword(&mut current, "hile", TokenType::While),
            _ => TokenType::Identifier,
        }
    }

    fn number(&mut self) -> Token {
        while let Some(c) = self.source_iter.peek()
            && c.is_ascii_digit()
        {
            self.advance();
        }

        // Look for the decimal point
        if let Some('.') = self.source_iter.peek()
            && let Some(c) = self.source_iter.peek_next()
            && c.is_ascii_digit()
        {
            self.advance(); // Consume the .

            while let Some(c) = self.source_iter.peek()
                && c.is_ascii_digit()
            {
                self.advance();
            }
        }

        self.make_token(TokenType::Number)
    }
}

fn is_alpha(c: char) -> bool {
    c.is_ascii_alphabetic() || c == '_'
}

fn check_keyword(current: &mut Chars, rest: &'static str, typ: TokenType) -> TokenType {
    let mut rest2 = rest.chars();
    loop {
        match (current.next(), rest2.next()) {
            (None, None) => return typ,
            (Some(c1), Some(c2)) if c1 == c2 => continue,
            _ => break,
        }
    }

    TokenType::Identifier
}
