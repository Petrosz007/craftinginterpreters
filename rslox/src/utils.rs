/// Just like std::iter::Peekable, but can peek 2 in advance
pub struct Peeknextable<I: Iterator> {
    iter: I,
    /// Remember a peeked value, even if it was None.
    peeked: Option<Option<I::Item>>,
    /// Remember a peek nexted value, even if it was None.
    peeknexted: Option<Option<I::Item>>,
}

impl<I: Iterator> Peeknextable<I> {
    fn new(iter: I) -> Peeknextable<I> {
        Peeknextable {
            iter,
            peeked: None,
            peeknexted: None,
        }
    }
}

impl<I: Iterator> Iterator for Peeknextable<I> {
    type Item = I::Item;

    #[inline]
    fn next(&mut self) -> Option<I::Item> {
        match self.peeked.take() {
            Some(v) => {
                self.peeked = self.peeknexted.take();
                v
            }
            None => self.iter.next(),
        }
    }
}

impl<I: Iterator> Peeknextable<I> {
    #[inline]
    pub fn peek(&mut self) -> Option<&I::Item> {
        let iter = &mut self.iter;
        self.peeked.get_or_insert_with(|| iter.next()).as_ref()
    }

    #[inline]
    pub fn peek_next(&mut self) -> Option<&I::Item> {
        let iter = &mut self.iter;
        self.peeked.get_or_insert_with(|| iter.next());
        self.peeknexted.get_or_insert_with(|| iter.next()).as_ref()
    }
}

// TODO: Write unit tests for this iterator

pub trait UtilsIterator: Iterator {
    fn peeknextable(self) -> Peeknextable<Self>
    where
        Self: Sized,
    {
        Peeknextable::new(self)
    }
}

impl<T> UtilsIterator for T where T: Iterator + ?Sized {}
