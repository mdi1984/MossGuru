# MossGuru
A simple .NET client for Moss (http://moss.stanford.edu/)

Use MossGuru to submit more complex directory structures.
- Submission
  - Student A
    - Porgram.Domain
      - DomainObject1.cs
      - DomainObject2.cs
    - Program.Services
      - Interfaces
        - IService.cs
      - Service.cs
    - ...
  - Student B
    - ...
    
Originally most submission scripts treat every folder as its own submission. MossGuru simply takes every file under the first hierarchy (Students), replaces the slashes in its path with underlindes and sends them to the Moss server.
Plus it reports progress and allows filtering by file extensions ;-)

      
