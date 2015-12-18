﻿namespace MonoDevelopTests
open System
open NUnit.Framework
open MonoDevelop.FSharp
open FsUnit

[<TestFixture>]
type FSharpUnitTestTextEditorExtensionTests() =
    let gatherTests (text:string) =
      let editor = TestHelpers.createDoc text ""
      let ast = editor.Ast
      let symbols = ast.GetAllUsesOfAllSymbolsInFile() |> Async.RunSynchronously

      unitTestGatherer.gatherUnitTests (editor.Editor, symbols) 
      |> Seq.toList

    let gatherTestsWithReference (text:string) =
        let attributes = """
namespace NUnit.Framework
open System
type TestAttribute() =
  inherit Attribute()
type TestFixtureAttribute() =
  inherit Attribute()
type IgnoreAttribute() =
  inherit Attribute()
type TestCaseAttribute(arg:obj) =
  inherit Attribute()
"""
        gatherTests (attributes + text)

    [<Test>]
    member x.BasicTestCoveringNormalAndDoubleQuotedTestsInATestFixture () =
        let normalAndDoubleTick = """
open System
open NUnit.Framework
[<TestFixture>]
type Test() =
    [<Test>]
    member x.TestOne() = ()

    [<Test>]
    [<Ignore>]
    member x.``Test Two``() = ()
"""
        let res = gatherTestsWithReference normalAndDoubleTick
        match res with
        | [fixture;t1;t2] -> 
            fixture.IsFixture |> should equal true
            fixture.UnitTestIdentifier |> should equal "NUnit.Framework.Test"

            t1.UnitTestIdentifier |> should equal "NUnit.Framework.Test.TestOne"
            t1.IsIgnored |> should equal false

            t2.UnitTestIdentifier |> should equal "NUnit.Framework.Test.``Test Two``"
            t2.IsIgnored |> should equal true
        | _ -> NUnit.Framework.Assert.Fail "invalid number of tests returned"

    [<Test>]
    member x.NoTests () =
        let noTests = """
open System
open NUnit.Framework

type Test() =
    member x.TestOne() = ()
"""

        let tests = gatherTestsWithReference noTests
        tests.Length |> should equal 0
    
    [<Test>]
    member x.``Module tests without TestFixtureAttribute are detected`` () =
        let noTests = """
module someModule =

  open NUnit.Framework

  [<Test>]
  let atest () =
      ()
"""

        let tests = gatherTestsWithReference noTests
        tests.Length |> should equal 1

    [<Test>]
    member x.NestedTestCoveringNormalAndDoubleQuotedTestsInATestFixture () =
        let nestedTests = """
open System
open NUnit.Framework
module Test =
    [<TestFixture>]
    type Test() =
        [<Test>]
        member x.TestOne() = ()

        [<Test>]
        [<Ignore>]
        member x.``Test Two``() = ()
"""
        let tests = gatherTestsWithReference nestedTests

        match tests with
        | [fixture;t1;t2] -> 
            fixture.IsFixture |> should equal true
            fixture.UnitTestIdentifier |> should equal "NUnit.Framework.Test+Test"

            t1.UnitTestIdentifier |> should equal "NUnit.Framework.Test+Test.TestOne"
            t1.IsIgnored |> should equal false

            t2.UnitTestIdentifier |> should equal "NUnit.Framework.Test+Test.``Test Two``"
            t2.IsIgnored |> should equal true
        | _ -> NUnit.Framework.Assert.Fail "invalid number of tests returned"

    [<Test>]
    member x.TestsPresentButNoNUnitReference () =
        let normalAndDoubleTick = """
open System
open NUnit.Framework
[<TestFixture>]
type Test() =
    [<Test>]
    member x.TestOne() = ()

    [<Test>]
    [<Ignore>]
    member x.``Test Two``() = ()
"""
        let tests = gatherTests normalAndDoubleTick

        tests.Length |> should equal 0

    [<Test>]
    member x.``Shows test case identifier`` () =
        let fixture = """
open NUnit.Framework
[TestFixture]
type Fixture() =
  [<Test>]
  [<TestCase(1)>]
  member x.atest (i) =
    ()
"""
        let tests = gatherTestsWithReference fixture

        tests.[0].UnitTestIdentifier |> should equal "atest(1)"
