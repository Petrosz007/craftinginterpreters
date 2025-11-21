package com.github.petrosz007.craftinginterpreters.slox

import scala.collection.mutable.ArrayBuffer

package object values {
  opaque type Value = Double
  object Value:
    def apply(x: Double): Value = x
}
