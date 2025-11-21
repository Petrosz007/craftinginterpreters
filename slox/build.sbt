ThisBuild / version := "0.1.0-SNAPSHOT"

ThisBuild / scalaVersion := "3.4.2"

lazy val root = (project in file("."))
  .settings(
    organization := "com.github.petrosz007.craftinginterpreters",
    name := "slox",
  )
