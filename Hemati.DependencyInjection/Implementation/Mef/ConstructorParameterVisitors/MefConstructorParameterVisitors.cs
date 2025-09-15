// SPDX-License-Identifier: LGPL-3.0-only

namespace Hemati.DependencyInjection.Implementation.Mef.ConstructorParameterVisitors;

public static class MefConstructorParameterVisitors
{
 public static IConstructorParameterVisitor[] GetVisitors()
 {
  return new IConstructorParameterVisitor[] { new MefImportManyVisitor() };
 }
}