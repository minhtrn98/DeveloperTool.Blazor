using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using TMS.DeveloperTool.Blazor.Features.ApiRequest.Services;

namespace TMS.DeveloperTool.Blazor.Tests.Features.ApiRequest;

public class ApiRequestSwaggerServiceTests
{
    private const string SwaggerJson = """
    {
      "openapi": "3.0.4",
      "info": {
        "title": "Order API",
        "version": "v1"
      },
      "paths": {
        "/api/v1/orders/search": {
          "post": {
            "tags": ["Order"],
            "requestBody": {
              "content": {
                "application/json": {
                  "schema": {
                    "$ref": "#/components/schemas/SearchOrdersRequest"
                  }
                }
              }
            }
          }
        },
        "/api/v1/orders/{id}": {
          "put": {
            "tags": ["Order"],
            "summary": "Update order",
            "parameters": [
              {
                "name": "id",
                "in": "path",
                "required": true,
                "schema": {
                  "type": "string",
                  "format": "uuid"
                }
              },
              {
                "name": "includeDetails",
                "in": "query",
                "schema": {
                  "type": "boolean"
                }
              }
            ],
            "requestBody": {
              "content": {
                "application/json": {
                  "schema": {
                    "type": "object",
                    "properties": {
                      "name": {
                        "type": "string"
                      },
                      "priority": {
                        "type": "integer"
                      }
                    }
                  }
                }
              }
            }
          }
        }
      },
      "components": {
        "schemas": {
          "SearchOrdersRequest": {
            "type": "object",
            "properties": {
              "searchTerm": {
                "type": "string"
              },
              "statuses": {
                "type": "array",
                "items": {
                  "type": "integer"
                }
              },
              "paging": {
                "$ref": "#/components/schemas/PagingRequest"
              },
              "rescheduledPickupDt": {
                "type": "string",
                "format": "date-time"
              }
            }
          },
          "PagingRequest": {
            "type": "object",
            "properties": {
              "page": {
                "type": "integer",
                "default": 1
              },
              "pageSize": {
                "type": "integer",
                "default": 20
              }
            }
          }
        }
      }
    }
    """;

    [Fact]
    public void Parse_ShouldExtractEndpointsAndGenerateSampleRequestBody()
    {
        ApiRequestSwaggerService service = new(Mock.Of<IWebHostEnvironment>());

        SwaggerDocumentInfo document = service.Parse("Order", SwaggerJson);

        document.Title.Should().Be("Order API");
        document.Tags.Should().ContainSingle().Which.Should().Be("Order");
        document.Endpoints.Should().HaveCount(2);

        SwaggerEndpointOption searchEndpoint = document.Endpoints.Single(x => x.Key == "POST:/api/v1/orders/search");
        using JsonDocument sampleDocument = JsonDocument.Parse(searchEndpoint.SampleRequestBody);
        sampleDocument.RootElement.GetProperty("searchTerm").GetString().Should().Be("string");
        sampleDocument.RootElement.GetProperty("statuses")[0].GetInt64().Should().Be(0);
        sampleDocument.RootElement.GetProperty("paging").GetProperty("page").GetInt64().Should().Be(1);
        sampleDocument.RootElement.GetProperty("paging").GetProperty("pageSize").GetInt64().Should().Be(20);
        sampleDocument.RootElement.GetProperty("rescheduledPickupDt").GetString().Should().Be("2026-01-01T00:00:00Z");
    }

    [Fact]
    public void Parse_ShouldApplyPathAndQueryParameterSamplesToEndpointPath()
    {
        ApiRequestSwaggerService service = new(Mock.Of<IWebHostEnvironment>());

        SwaggerDocumentInfo document = service.Parse("Order", SwaggerJson);

        SwaggerEndpointOption updateEndpoint = document.Endpoints.Single(x => x.Key == "PUT:/api/v1/orders/{id}");
        updateEndpoint.Path.Should().Be("/api/v1/orders/00000000-0000-0000-0000-000000000000?includeDetails=false");
        updateEndpoint.Description.Should().Be("Update order");
        updateEndpoint.Parameters.Should().HaveCount(2);
    }
}