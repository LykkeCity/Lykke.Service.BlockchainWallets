using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.BlockchainWallets.Client;
using Lykke.Service.BlockchainWallets.Contract.Models;
using Newtonsoft.Json;
using Xunit;

namespace Lykke.Service.BlockchainWallets.Tests.Client
{
    public class BlockchainWalletsClientTests
    {
        [Fact]
        public async Task Any_Method_Called__Input_Parameters_Are_Invalid__Exception_Thrown()
        {
            var handlerStub = new DelegatingHandlerStub();
            var client = CreateClient(handlerStub);

            var integrationLayerIdCases = new[]
            {
                new { Case = (string) null, IsValid = false },
                new { Case = "", IsValid = false },
                new { Case = "EthereumClassic", IsValid = true }
            };

            var integrationLayerAssetIdCases = new[]
            {
                new { Case = (string) null, IsValid = false },
                new { Case = "", IsValid = false },
                new { Case = "ETC", IsValid = true }
            };

            var clientIdCases = new[]
            {
                new {Case = Guid.Empty, IsValid = false},
                new {Case = Guid.NewGuid(), IsValid = true}
            };

            var clientActions = new List<Func<string, string, Guid, Task>>
            {
                async (a, b, c) => { await client.CreateWalletAsync(a, b, c); },
                async (a, b, c) => { await client.DeleteWalletAsync(a, b, c); },
                async (a, b, c) => { await client.GetAddressAsync(a, b, c); },
                async (a, b, c) => { await client.TryGetAddressAsync(a, b, c); },
            };

            foreach (var clientAction in clientActions)
            {
                foreach (var integrationLayerIdCase in integrationLayerIdCases)
                {
                    foreach (var integrationLayerAssetIdCase in integrationLayerAssetIdCases)
                    {
                        foreach (var clientIdCase in clientIdCases)
                        {
                            if (!integrationLayerIdCase.IsValid &&
                                !integrationLayerAssetIdCase.IsValid &&
                                !clientIdCase.IsValid)
                            {
                                try
                                {
                                    await clientAction
                                    (
                                        integrationLayerAssetIdCase.Case,
                                        integrationLayerAssetIdCase.Case,
                                        clientIdCase.Case
                                    );
                                }
                                catch (Exception e)
                                {
                                    Assert.IsType<ArgumentException>(e);
                                }
                            }
                        }
                    }
                }
            }

            var addressActions = new List<Func<string, string, string, Task>>
            {
                async (a, b, c) => { await client.GetClientIdAsync(a, b, c); },
                async (a, b, c) => { await client.TryGetClientIdAsync(a, b, c); }
            };

            var addressCases = new[]
            {
                new { Case = (string) null, IsValid = false },
                new { Case = "", IsValid = false },
                new { Case = "0x83f0726180cf3964b69f62ac063c5cb9a66b3be5", IsValid = true }
            };

            foreach (var addressAction in addressActions)
            {
                foreach (var integrationLayerIdCase in integrationLayerIdCases)
                {
                    foreach (var integrationLayerAssetIdCase in integrationLayerAssetIdCases)
                    {
                        foreach (var addressCase in addressCases)
                        {
                            if (!integrationLayerIdCase.IsValid &&
                                !integrationLayerAssetIdCase.IsValid &&
                                !addressCase.IsValid)
                            {
                                try
                                {
                                    await addressAction
                                    (
                                        integrationLayerAssetIdCase.Case,
                                        integrationLayerAssetIdCase.Case,
                                        addressCase.Case
                                    );
                                }
                                catch (Exception e)
                                {
                                    Assert.IsType<ArgumentException>(e);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task GetIsAliveAsync_Called__ServiceIsHealthy_IsAliveResponse_Returned()
        {
            var handlerStub = new DelegatingHandlerStub(HttpStatusCode.OK, new IsAliveResponse());
            var client = CreateClient(handlerStub);
            var response = await client.GetIsAliveAsync();

            Assert.IsType<IsAliveResponse>(response);
        }

        [Fact]
        public async Task GetIsAliveAsync_Called__ServiceIsUnhealthy_Exception_Thrown()
        {
            var handlerStub = new DelegatingHandlerStub(HttpStatusCode.InternalServerError, ErrorResponse.Create(""));
            var client = CreateClient(handlerStub);

            try
            {
                await client.GetIsAliveAsync();
            }
            catch (Exception e)
            {
                Assert.IsType<ErrorResponseException>(e);
            }
        }

        [Fact]
        public async Task CreateWalletAsync_Called__Asset_Is_Not_Supported__Exception_Thrown()
        {
            var handlerStub = new DelegatingHandlerStub(HttpStatusCode.BadRequest, new ErrorResponse());
            var client = CreateClient(handlerStub);

            try
            {
                await client.CreateWalletAsync("EthereumClassic", "ETC", Guid.NewGuid());
            }
            catch (Exception e)
            {
                Assert.IsType<ErrorResponseException>(e);
            }
        }

        [Fact]
        public async Task CreateWalletAsync_Called__Wallet_Exists__Null_Returned()
        {
            var handlerStub = new DelegatingHandlerStub(HttpStatusCode.Conflict, new ErrorResponse());
            var client = CreateClient(handlerStub);
            var response = await client.CreateWalletAsync("EthereumClassic", "ETC", Guid.NewGuid());

            Assert.Null(response);
        }

        [Fact]
        public async Task DeleteWalletAsync_Called__Asset_Is_Not_Supported__Exception_Thrown()
        {
            var handlerStub = new DelegatingHandlerStub(HttpStatusCode.BadRequest, new ErrorResponse());
            var client = CreateClient(handlerStub);

            try
            {
                await client.DeleteWalletAsync("EthereumClassic", "ETC", Guid.NewGuid());
            }
            catch (Exception e)
            {
                Assert.IsType<ErrorResponseException>(e);
            }
        }

        [Fact]
        public async Task DeleteWalletAsync_Called__Wallet_Exists__True_Returned()
        {
            var handlerStub = new DelegatingHandlerStub(HttpStatusCode.Accepted);
            var client = CreateClient(handlerStub);
            var response = await client.DeleteWalletAsync("EthereumClassic", "ETC", Guid.NewGuid());

            Assert.True(response);
        }

        [Fact]
        public async Task DeleteWalletAsync_Called__Wallet_Exists__False_Returned()
        {
            var handlerStub = new DelegatingHandlerStub(HttpStatusCode.NotFound, new ErrorResponse());
            var client = CreateClient(handlerStub);
            var response = await client.DeleteWalletAsync("EthereumClassic", "ETC", Guid.NewGuid());

            Assert.False(response);
        }

        [Fact]
        public async Task GetAddressAsync_Called__Client_Exists__Address_Returned()
        {
            var address = "0x83f0726180cf3964b69f62ac063c5cb9a66b3be5";
            var handlerStub = new DelegatingHandlerStub(HttpStatusCode.OK, new AddressResponse
            {
                Address = address
            });

            var client = CreateClient(handlerStub);
            var response = await client.GetAddressAsync("EthereumClassic", "ETC", Guid.NewGuid());

            Assert.Equal(address, response.Address);
        }

        [Fact]
        public async Task GetAddressAsync_Called__Client_Not_Exists__Exception_Thrown()
        {
            var handlerStub = new DelegatingHandlerStub(HttpStatusCode.NoContent);
            var client = CreateClient(handlerStub);

            try
            {
                await client.GetAddressAsync("EthereumClassic", "ETC", Guid.NewGuid());
            }
            catch (Exception e)
            {
                Assert.IsType<ResultValidationException>(e);
            }
        }

        [Fact]
        public async Task GetClientIdAsync_Called__Client_Exists__ClientId_Returned()
        {
            var clientId = Guid.NewGuid();
            var handlerStub = new DelegatingHandlerStub(HttpStatusCode.OK, new ClientIdResponse
            {
                ClientId = clientId
            });

            var client = CreateClient(handlerStub);
            var response = await client.GetClientIdAsync("EthereumClassic", "ETC", "0x83f0726180cf3964b69f62ac063c5cb9a66b3be5");

            Assert.Equal(clientId, response);
        }

        [Fact]
        public async Task GetClientIdAsync_Called__Client_Not_Exists__Exception_Thrown()
        {
            var handlerStub = new DelegatingHandlerStub(HttpStatusCode.NoContent);
            var client = CreateClient(handlerStub);

            try
            {
                await client.GetClientIdAsync("EthereumClassic", "ETC", "0x83f0726180cf3964b69f62ac063c5cb9a66b3be5");
            }
            catch (Exception e)
            {
                Assert.IsType<ResultValidationException>(e);
            }
        }

        [Fact]
        public async Task GetClientWalletsAsync_Called__Wallets_Exists__Return_ClientWalletResponse()
        {
            var clientId = Guid.Parse("25c47ff8-e31e-4913-8e02-8c2512f0111e");
            var handlerStub = new DelegatingHandlerStub(HttpStatusCode.OK, new WalletsResponse()
            {
                ContinuationToken = null,
                Wallets = new[]
                {
                    new WalletResponse()
                    {
                        Address = "0x00000000...",
                        ClientId = clientId,
                        BlockchainType = "EthereumClassic",
                        IntegrationLayerAssetId = "ETC"
                    }
                }

            });
            var client = CreateClient(handlerStub);

            var result = await client.GetWalletsAsync(clientId, 10, null);

            Assert.True(result?.Wallets.FirstOrDefault()?.ClientId == clientId);
        }

        [Fact]
        public async Task GetClientWalletsAsync_Called__Wallets_Exists__Return_Many_ClientWalletResponse()
        {
            var clientId = Guid.Parse("25c47ff8-e31e-4913-8e02-8c2512f0111e");
            var counter = 0;
            
            #region Responses

            var content1 = new WalletsResponse()
            {
                ContinuationToken = "1",
                Wallets = new[]
                {
                    new WalletResponse()
                    {
                        Address = "0x00000000...",
                        ClientId = clientId,
                        BlockchainType = "EthereumClassic",
                        IntegrationLayerAssetId = "ETC"
                    }
                }
            };

            var content2 = new WalletsResponse()
            {
                ContinuationToken = null,
                Wallets = new[]
                {
                    new WalletResponse()
                    {
                        Address = "0x00000001...",//Does not matter
                        ClientId = clientId,
                        BlockchainType = "LiteCoin",
                        IntegrationLayerAssetId = "LTC"
                    }
                }
            };

            #endregion

            var handlerStub = new DelegatingHandlerStub((request, cancellationToken) =>
            {
                var content = content1;
                if (counter > 0)
                {
                    content = content2;
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(content))
                };

                counter++;

                return Task.FromResult(response);
            });

            var client = CreateClient(handlerStub);

            var result = await client.GetAllWalletsAsync(clientId, 1);

            Assert.True(result?.Count() == 2);
            Assert.True(result?.FirstOrDefault()?.ClientId == clientId);
        }


        private static BlockchainWalletsClient CreateClient(HttpMessageHandler handlerStub)
        {
            var httpClient = new HttpClient(handlerStub)
            {
                BaseAddress = new Uri("http://localhost")
            };

            return new BlockchainWalletsClient(httpClient);
        }

        private static BlockchainWalletsClient CreateClient()
        {
            var httpClient = new HttpClient()
            {
                BaseAddress = new Uri("http://localhost:5000")
            };

            return new BlockchainWalletsClient(httpClient);
        }
    }
}
