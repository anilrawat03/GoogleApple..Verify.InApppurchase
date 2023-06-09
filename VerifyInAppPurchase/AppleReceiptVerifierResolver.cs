﻿using System;
using System.Collections.Concurrent;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace VerifyInAppPurchase
{
    public interface IAppleReceiptVerifierResolver
    {
        IInAppPurchaseReceiptVerifier Resolve(string verifierName);
    }

    public class AppleReceiptVerifierResolver : IAppleReceiptVerifierResolver
    {
        readonly IServiceProvider _services;
        readonly IHttpClientFactory _httpClientFactory;

        readonly ConcurrentDictionary<string,
            IInAppPurchaseReceiptVerifier> _createdVerifiers = new ConcurrentDictionary<string, IInAppPurchaseReceiptVerifier>();

        public AppleReceiptVerifierResolver(IServiceProvider services)
        {
            _services = services;
            _httpClientFactory = _services.GetRequiredService<IHttpClientFactory>();
        }

        public IInAppPurchaseReceiptVerifier Resolve(string verifierName)
        {
            if (verifierName == AppleReceiptVerifierOptions.DefaultVerifierName)
                throw new InvalidOperationException("Resolver can only be used when registering service implementations with non-default names.");
            if (_createdVerifiers.TryGetValue(verifierName, out var verifier))
                return verifier;
            verifierName = AppleReceiptVerifierOptions.ServicesPrefix + verifierName;
            var namedOptionsResolver = _services.GetRequiredService<IOptionsSnapshot<AppleReceiptVerifierOptions>>();
            var namedOptions = namedOptionsResolver.Get(verifierName);
            var options = Options.Create<AppleReceiptVerifierOptions>(namedOptions);
            var httpClient = _httpClientFactory.CreateClient(verifierName);
            var instance = ActivatorUtilities.CreateInstance<InAppPurchaseReceiptVerifier>(_services, options, httpClient);
            return _createdVerifiers.GetOrAdd(verifierName, instance);
        }
    }
}