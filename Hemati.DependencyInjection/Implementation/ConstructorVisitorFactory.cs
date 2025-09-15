// SPDX-License-Identifier: LGPL-3.0-only

namespace Hemati.DependencyInjection.Implementation;

public class ConstructorVisitorFactory(IConstructorParameterVisitor[] parameterVisitors)
{
 public ConstructorVisitor Produce(ServicesDescriptor servicesDescriptor) => new(servicesDescriptor, parameterVisitors);
}