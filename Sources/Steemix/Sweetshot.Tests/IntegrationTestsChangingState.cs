﻿using System;
using System.Configuration;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sweetshot.Library.HttpClient;
using Sweetshot.Library.Models.Common;
using Sweetshot.Library.Models.Requests;

namespace Sweetshot.Tests
{
    [TestFixture]
    public class IntegrationTestsChangingState
    {
        private const string Name = "joseph.kalu";
        private const string Password = "test1234";
        private const string NewPassword = "test12345";
        private string _sessionId = string.Empty;

        private readonly SteepshotApiClient _api = new SteepshotApiClient(ConfigurationManager.AppSettings["sweetshot_url"]);

        [SetUp]
        public void Authenticate()
        {
            var request = new LoginRequest(Name, Password);
            _sessionId = _api.Login(request).Result.Result.SessionId;
        }

        [Test]
        [Order(0)]
        public void Upload()
        {
            // Arrange
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Data\cat.jpg");
            var file = File.ReadAllBytes(path);
            var request = new UploadImageRequest(_sessionId, "cat" + DateTime.UtcNow.Ticks, file, "cat1", "cat2", "cat3", "cat4");

            // Act
            var response = _api.Upload(request).Result;

            // Assert
            AssertSuccessfulResult(response);
            Assert.That(response.Result.Body, Is.Not.Empty);
            Assert.That(response.Result.Title, Is.Not.Empty);
            Assert.That(response.Result.Tags, Is.Not.Empty);
        }

        [Test]
        [Order(1)]
        public void CreateComment()
        {
            // Arrange
            var request = new CreateCommentRequest(_sessionId, "@joseph.kalu/cat636203355240074655", "nailed it !", "свитшот");

            // Act
            var response = _api.CreateComment(request).Result;

            // Assert
            AssertSuccessfulResult(response);
            Assert.That(response.Result.IsCreated, Is.True);
            Assert.That(response.Result.Message, Is.EqualTo("Comment created"));
        }

        [Test]
        [Order(2)]
        public void Vote_Up()
        {
            // Prepare
            var userPostsRequest = new UserPostsRequest(Name);
            var userPostsResponse = _api.GetUserPosts(userPostsRequest).Result;
            AssertSuccessfulResult(userPostsResponse);
            Assert.That(userPostsResponse.Result.Count, Is.Not.Null);
            Assert.That(userPostsResponse.Result.Offset, Is.Not.Empty);
            Assert.That(userPostsResponse.Result.Results, Is.Not.Empty);

            // Arrange
            var url = userPostsResponse.Result.Results.First().Url;
            var request = new VoteRequest(_sessionId, true, url);

            // Act
            var response = _api.Vote(request).Result;

            // Assert
            AssertSuccessfulResult(response);
            Assert.That(response.Result.IsVoted, Is.True);
            Assert.That(response.Result.NewTotalPayoutReward, Is.Not.Null);
            Assert.That(response.Result.Message, Is.EqualTo("Upvoted"));
            Assert.That(response.Result.NewTotalPayoutReward, Is.Not.Null);
        }

        [Test]
        [Order(3)]
        public void Vote_Down()
        {
            // Prepare
            var userPostsRequest = new UserPostsRequest(Name);
            var userPostsResponse = _api.GetUserPosts(userPostsRequest).Result;
            AssertSuccessfulResult(userPostsResponse);
            Assert.That(userPostsResponse.Result.Count, Is.Not.Null);
            Assert.That(userPostsResponse.Result.Offset, Is.Not.Empty);
            Assert.That(userPostsResponse.Result.Results, Is.Not.Empty);

            // Arrange
            var url = userPostsResponse.Result.Results.First().Url;

            // Arrange
            var request = new VoteRequest(_sessionId, false, url);

            // Act
            var response = _api.Vote(request).Result;

            // Assert
            AssertSuccessfulResult(response);
            Assert.That(response.Result.IsVoted, Is.False);
            Assert.That(response.Result.NewTotalPayoutReward, Is.Not.Null);
            Assert.That(response.Result.Message, Is.EqualTo("Downvoted"));
            Assert.That(response.Result.NewTotalPayoutReward, Is.Not.Null);
        }

        [Test]
        [Order(4)]
        public void Follow()
        {
            // Arrange
            var request = new FollowRequest(_sessionId, FollowType.Follow, "asduj");

            // Act
            var response = _api.Follow(request).Result;

            // Assert
            AssertSuccessfulResult(response);
            Assert.That(response.Result.IsFollowed, Is.True);
            Assert.That(response.Result.Message, Is.EqualTo("User is followed"));
        }

        [Test]
        [Order(5)]
        public void Follow_UnFollow()
        {
            // Arrange
            var request = new FollowRequest(_sessionId, FollowType.UnFollow, "asduj");

            // Act
            var response = _api.Follow(request).Result;

            // Assert
            AssertSuccessfulResult(response);
            Assert.That(response.Result.IsFollowed, Is.False);
            Assert.That(response.Result.Message, Is.EqualTo("User is unfollowed"));
        }

        [Test]
        [Order(6)]
        public void Logout()
        {
            // Arrange
            var request = new LogoutRequest(_sessionId);

            // Act
            var response = _api.Logout(request).Result;

            // Assert
            AssertSuccessfulResult(response);
            Assert.That(response.Result.IsLoggedOut, Is.True);
            Assert.That(response.Result.Message, Is.EqualTo("User is logged out"));
        }

        // TODO Need to create a profile and test it
        [Ignore("Ignoring")]
        [Order(7)]
        public void Register()
        {
            // Arrange
            var request = new RegisterRequest("", "", "");

            // Act
            var response = _api.Register(request).Result;

            // Assert
            //Assert.That(response.Result.IsLoggedIn, Is.False);
            //AssertSuccessfulResult(response);
            //Assert.That(response.Result.SessionId);
            //Assert.That(response.Result.Username);
        }

        [Test]
        [Order(8)]
        public void ChangePassword()
        {
            // Arrange
            var request = new ChangePasswordRequest(_sessionId, Password, NewPassword);

            // Act
            var response = _api.ChangePassword(request).Result;

            // Assert
            AssertSuccessfulResult(response);
            Assert.That(response.Result.IsChanged, Is.True);
            Assert.That(response.Result.Message, Is.EqualTo("Password was changed"));

            // Revert
            var loginResponse = _api.Login(new LoginRequest(Name, NewPassword)).Result;
            var response2 = _api.ChangePassword(new ChangePasswordRequest(loginResponse.Result.SessionId, NewPassword, Password)).Result;
            AssertSuccessfulResult(response2);
            Assert.That(response.Result.IsChanged, Is.True);
            Assert.That(response2.Result.Message, Is.EqualTo("Password was changed"));
            Authenticate();
        }

        private void AssertSuccessfulResult<T>(OperationResult<T> response)
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Success, Is.True);
            Assert.That(response.Result, Is.Not.Null);
            Assert.That(response.Errors, Is.Empty);
        }
    }
}