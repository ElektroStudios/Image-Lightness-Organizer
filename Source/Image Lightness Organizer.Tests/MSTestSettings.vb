#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

' Test classes run in parallel; test methods within the same class run sequentially.
' Every test that touches the file system uses its own unique temporary directory.
<Assembly: Parallelize(Workers:=0, Scope:=ExecutionScope.ClassLevel)>
