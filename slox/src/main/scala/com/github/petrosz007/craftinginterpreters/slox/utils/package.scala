package com.github.petrosz007.craftinginterpreters.slox

import scala.util.{Failure, Success, Try}

package object utils {
  object OptionExtension:
    extension [A](option: Option[A])
      def orFailWith(throwable: => Throwable): Try[A] =
        option.map(Success(_)).getOrElse(Failure(throwable))

  enum Continue:
    case Yes, No
}
