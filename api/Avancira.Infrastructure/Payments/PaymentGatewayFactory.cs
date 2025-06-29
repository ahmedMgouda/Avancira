using Avancira.Application.Billing;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Avancira.Infrastructure.Billing
{
    public class PaymentGatewayFactory : IPaymentGatewayFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public PaymentGatewayFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IPaymentGateway GetPaymentGateway(string gatewayName)
        {
            return gatewayName?.ToLower() switch
            {
                "stripe" => _serviceProvider.GetService<IPaymentGateway>(), // You can implement specific gateways later
                "paypal" => _serviceProvider.GetService<IPaymentGateway>(), // You can implement specific gateways later
                _ => throw new NotSupportedException($"Payment gateway '{gatewayName}' is not supported.")
            };
        }
    }
}
