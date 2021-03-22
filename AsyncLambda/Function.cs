using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using OrderOrchestration;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AsyncLambda
{
    public class Payload
    {
        public bool IsApproved { get; set; }

        public string OrderID { get; set; }
    }



    public class Function
    {
        
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine(request.Body);
            var input = JsonSerializer.Deserialize<Payload>(request.Body);
            context.Logger.LogLine(JsonSerializer.Serialize(input.OrderID));

            //Get Order details
            List<Order> allDocs = await GetOrderDetails(input);
            var savedState = allDocs.FirstOrDefault();

            //Callback step function
            SendTaskSuccessResponse sendTaskSuccessResponse;
            using (var amazonStepFunctionsClient = new AmazonStepFunctionsClient())
            {
                //Set Stepfunction output data.
                var data = new
                {
                    IsApproved = input.IsApproved
                };
                var jsonData = JsonSerializer.Serialize(data);
                var startExecutionRequest = new StartExecutionRequest
                {
                    Input = jsonData,
                };
                var result = amazonStepFunctionsClient.SendTaskSuccessAsync(new SendTaskSuccessRequest() { TaskToken = savedState.Token, Output = jsonData }).Result;
            }
            APIGatewayProxyResponse response = new APIGatewayProxyResponse() { Body = "Successfully invoked stepfunction.", StatusCode = 200 };
            return response;
        }

        private static async Task<List<Order>> GetOrderDetails(Payload input)
        {
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();
            var dbCcontext = new DynamoDBContext(client);
            List<ScanCondition> conditions = new List<ScanCondition>();
            conditions.Add(new ScanCondition("OrderID", ScanOperator.Equal, input.OrderID));
            var allDocs = await dbCcontext.ScanAsync<Order>(conditions).GetRemainingAsync();
            return allDocs;
        }

        private async void SaveOrder()
        {
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();
            Table orderTable = Table.LoadTable(client, "Orders");
            var orderData = new Document();
            orderData["OrderID"] = "11222"+ Guid.NewGuid().ToString();
            orderData["Token"] = Guid.NewGuid().ToString();
            var result = await orderTable.PutItemAsync(orderData);
            Console.WriteLine(JsonSerializer.Serialize(result));
        }
    }
}


namespace OrderOrchestration
{
    public class Order
    {
        public string OrderID { get; set; }
        public List<string> OrderItems { get; set; }
        public int Cost { get; set; }
        public bool? IsApprovalRequired { get; set; }
        public bool? IsOrderValid { get; set; }
        public bool? IsApproved { get; set; }
        public string Token { get; set; }
    }
}
