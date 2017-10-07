/*
 * Copyright 2013-2016 consulo.io
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if UNITY_2017_2
[assembly: AssemblyTitle("UnityEditorConsuloPlugin2017.2")]
#elif UNITY_5_6
[assembly: AssemblyTitle("UnityEditorConsuloPlugin5.6")]
#elif NUNIT
[assembly: AssemblyTitle("UnityEditorConsuloPlugin5.3")]
#elif UNITY_BEFORE_5
[assembly: AssemblyTitle("UnityEditorConsuloPlugin4.6")]
#else
[assembly: AssemblyTitle("UnityEditorConsuloPlugin5")]
#endif

[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]

#if UNITY_2017_2
[assembly: AssemblyProduct("UnityEditorConsuloPlugin2017.2")]
#elif UNITY_5_6
[assembly: AssemblyProduct("UnityEditorConsuloPlugin5.6")]
#elif NUNIT
[assembly: AssemblyProduct("UnityEditorConsuloPlugin5.3")]
#elif UNITY_BEFORE_5
[assembly: AssemblyProduct("UnityEditorConsuloPlugin4.6")]
#else
[assembly: AssemblyProduct("UnityEditorConsuloPlugin5")]
#endif

[assembly: AssemblyCopyright("Copyright © consulo.io")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("d2344223-0a66-4f9b-9bef-a66c22160f3a")]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]