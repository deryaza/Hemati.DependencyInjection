// SPDX-License-Identifier: LGPL-3.0-only

using System.Reflection;

namespace Hemati.DependencyInjection.Implementation.Mef.ConstructorParameterVisitors;

public class MefImportManyVisitor : IConstructorParameterVisitor
{
 public void VisitParameter(ParameterInfo info, ref FindServiceRequest request)
 {
  if (info.CustomAttributes.Any(x => x.AttributeType.Name == "ImportManyAttribute"))
  {
   request.IsImportManyRequest = true;
  }
 }
}