using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace OrderOrchestration
{
    public class StepFunctionTasks
    {
        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public StepFunctionTasks()
        {
        }


        public Order Greeting(Order order, ILambdaContext context)
        {
            return order;
        }


        public async Task<Order> WaitForApprovalTask(Order order, ILambdaContext context)
        {
            AmazonDynamoDBClient client = new AmazonDynamoDBClient();
            var dbCcontext = new DynamoDBContext(client);
            List<ScanCondition> conditions = new List<ScanCondition>();
           
            conditions.Add(new ScanCondition("OrderID", ScanOperator.Equal, order.OrderID));
            var allDocs = await dbCcontext.ScanAsync<Order>(conditions).GetRemainingAsync();
            var savedState = allDocs.FirstOrDefault();
            savedState.Token = order.Token;
            //Update tocken in the DB..
            await dbCcontext.SaveAsync<Order>(savedState);
            return order;
        }


        public Order CallbackCompletedTask(Order order, ILambdaContext context)
        {

            return order;
        }

        public Order ValidateOrderTask(Order order, ILambdaContext context)
        {
            var isValid = false;
            order.IsOrderValid = isValid = order.OrderItems.Count > 0 && order.Cost > 0;
            if (isValid)
            {
                SaveOrder(order);
            }
            return order;
        }

        public Order ValidateCostTask(Order order, ILambdaContext context)
        {
            order.IsApprovalRequired = order.Cost > 100;
            return order;
        }
        public Order ProcessOrderTask(Order order, ILambdaContext context)
        {
            return order;
        }


        private async void SaveOrder(Order order)
        {
            using (AmazonDynamoDBClient client = new AmazonDynamoDBClient())
            {
                Table orderTable = Table.LoadTable(client, "Order");
                var orderData = new Document();
                orderData["OrderID"] = order.OrderID;
                orderData["OrderItems"] = order.OrderItems;
                orderData["Cost"] = order.Cost;
                orderData["IsApprovalRequired"] = order.IsApprovalRequired;
                orderData["IsOrderValid"] = order.IsOrderValid;
                orderData["IsApproved"] = order.IsApproved; ;
                orderData["Token"] = Guid.NewGuid().ToString();
                var result = await orderTable.PutItemAsync(orderData);
                Console.WriteLine(JsonSerializer.Serialize(result));
            }
        }

    }
}
