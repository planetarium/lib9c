# Contribution Guide

Thank you for your interest in contributing to lib9c!
Contributions from the community are essential to the success and growth of the project.
This document outlines the steps to contribute, the coding standards we follow, and how to submit
your contributions for review.

## What to contribute

### Documentation

Despite Lib9c has a large codebase, the lack of documents makes hard to overview whole project and
contribute.
Contributions are welcome, whether it's improving documentation, adding code comments, or even
fixing typos.

### Report issue

If you found an issue in Lib9c, feel free to report an issue to our project.
Just go to [Lib9c repository](https://github.com/planetarium/lib9c/issues) and push `New issue`
button to describe what you found.

### Code

You can also write your own code to improve Lib9c.
You can contribute both resolving existing issue and new improvements.

## How to contribute

### Fork and clone github repository

Lib9c is open source project, so you can fork Lib9c into your own account and can make changes.

1. On [Lib9c](https://github.com/planetarium/lib9c), click `Fork` button on top of the page to make
   your fork.
2. Clone repository to your local machine following this command
    ```shell
   $ git clone https://github.com/{your-name}/lib9c.git
   # or using github cli
   $ gh repo clone {your-name}/lib9c.git
   ```

### Create your own working branch

Lib9c's base working branch is `development`. In most cases, you can start your work from this
branch.
Following commands can help you to prepare your work.

```shell
$ cd lib9c
$ git checkout development
$ git pull origin development
$ git checkout -b {new-branch-name}  # Please check branch naming rule in appendix
```

Now you are ready to make your changes!

### Commit changes

Write your codes and leave commits.
We recommend to divide commits in small steps to see how your work flows and code changes.
Rebasing your work can be good strategy for clear work history.
If you make any changes, you must write/fix tests on your changes.

### Test

You can test your code integrity by running this command:

```shell
$ dotnet test
```

ALL the tests must be passed including all the prior tests.
If you have no tests for your new feature or break existing test, your contribution could be
rejected.

### Make pull request

Passed all tests? Now it's time to make a pull request to upstream repo.

1. Connect to github for your lib9c (https://github.com/{your-name}/lib9c/pulls) and
   hit `New pull request` button on top of the screen.
2. Set proper base and working branch.
3. Check your changes is right thing to make PR.
4. Click `Create pull request` and describe your work.
    - Please set assignee to you and label to represent your work.
5. Once you have done writing your work, click `Create pull request` and that's it.

## Contribution process

1. PR comes into upstream repository.
2. Assign main reviewer and review.
3. Improve PR communicating with contributor.
    - One or more change requests can be send to contributor.
4. Final decision to merge or reject PR.

## Appendix

### **Branch naming rule**

Lib9c has general branch naming rule which has `{prefix}/{body}/{suffix}` structure.
This branch naming rule is not forced but highly recommended to recognize which type of PR is yours.

#### Prefix

- feature
- bugfix

#### (Optional) Suffix

- date (e.g., YYYYMMDD)
- revision (e.g., some digits)
- related base version (e.g., v200210)

### Work with your local [Libplanet]

If you want to work with your local [Libplanet], you can fill `LibplanetDirectory` property in `Directory.Build.props` file.

When making a pull request, please do not include the `LibplanetDirectory` change.
