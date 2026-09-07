# Custom JSON Serializer & Deserializer

A minimal C# JSON serializer built from scratch without any JSON libraries or external dependencies to practice Reflection and understand how production serializers operate under the hood.

---

## Output

<p align="center">
  <img src="images/output.png" alt="Console Output Preview" />
</p>

---

> **Note:** This project is for practice, not for production. It only supports primitive types, strings, and custom nested objects. It does not handle types like enums or structs.

---

## Overview

I built this project to understand how production serializers like `System.Text.Json` work internally. Instead of treating serialization as a black box, I wanted to practice using C# Reflection to inspect types at runtime, read and set properties dynamically, and instantiate objects using constructors.

The project covers building a simple JSON parser, handling nested objects, formatting and writing JSON data to disk, reading JSON files asynchronously in chunks, and using custom attributes to control serialization behavior.

---

## Usage

### Serialization

```csharp
// Serialize a single object
Employee employee = new Employee("Abdullah", 20_000.50f, "Developer", "Dammam");
await JsonSerializer.SerializeAsync(employee, "employee.json");

// Serialize a collection of objects
Employee[] employees = { employee1, employee2 };
await JsonSerializer.SerializeAsync(employees, "employees.json");
```

### Deserialization

```csharp
// Deserialize a single object
Employee? singleEmp = await JsonSerializer.DeserializeAsync<Employee>("employee.json");

// Deserialize a list of objects
List<Employee> employeeList = await JsonSerializer.DeserializeListAsync<Employee>("employees.json");
```

---

## Custom Attributes

By default, public fields and properties are serialized, while private members and compiler-generated backing fields are ignored. The library provides four custom attributes to override this default behavior:

* **`[JsonPropertyName("Name")]`**: Overrides the default property or field name in the output JSON.
  ```csharp
  [JsonPropertyName("Wage")]
  public float Salary { get; set; }
  ```

* **`[JsonIgnore]`**: Excludes a public property or field that would otherwise be serialized by default.
  ```csharp
  [JsonIgnore]
  public int Experience { get; set; }
  ```

* **`[JsonInclude]`**: Includes a private property or field that would normally be ignored.
  ```csharp
  [JsonInclude]
  private bool IsActive { get; set; }
  ```

* **`[JsonConstructor]`**: Explicitly selects which constructor to invoke during deserialization when a class defines multiple public constructors.
  ```csharp
  [JsonConstructor]
  public Employee(string name, float salary)
  {
      // ...
  }
  ```

---

## Technical Implementation

### 1. Member Filtering & Reflection

Both serialization and deserialization rely on knowing exactly which fields and properties to read or write. `ReflectionHelper.GetMembers()` inspects the target class at runtime, using the `HandleRestrictions` delegate to filter out unwanted members via `Type.FindMembers()`:

```csharp
public static MemberInfo[] GetMembers(Type type)
{
    return type.FindMembers(
        MemberTypes.Property | MemberTypes.Field,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
        HandleRestrictions, null);
}
```

The `HandleRestrictions` filter enforces three rules:
* Excludes compiler-generated backing fields marked with `CompilerGeneratedAttribute`.
* Includes private fields and properties only if they are marked with `[JsonInclude]`.
* Excludes any field or property marked with `[JsonIgnore]`.

---

### 2. Serialization

Converting an object or collection into formatted JSON follows a six-step process:

1. **Inspecting the Object at Runtime:**  
   `GetOneObjectJson()` receives the object and uses `ReflectionHelper.GetMembers()` to get its allowed properties and fields. If the object has no serializable members, it returns `{}`.

2. **Preparing the JSON String:**  
   `ConvertMembersToJson()` creates a `StringBuilder` with proper indentation and starts preparing the `"key": value` pairs.

3. **Setting the Property Key:**  
   The member's key name is checked. If it has a `[JsonPropertyName]` attribute, it uses that custom name. Otherwise, it uses the original C# property name.

4. **Handling the Value:**  
   - **Standard Values:** If the value is a primitive, string, decimal, or null, `AppendMemberValue()` formats it directly (adding quotes around strings, formatting booleans in lowercase, or writing numbers as-is).
   - **User-Defined Types:** `HandleNestedObject()` checks if a property is a custom class (rather than a primitive or string). If it is not null, it recursively calls `GetOneObjectJson()` to serialize the inner object.

5. **Formatting the Final Output:**  
   If serializing a single object, the JSON string is closed with a closing brace (`}`). If serializing a group of objects, `ConvertElementsToJson()` places commas between each serialized object and wraps the entire group in square brackets (`[` and `]`).

6. **Writing to Disk Asynchronously:**  
   The formatted JSON string is written directly to the file using `StreamWriter.WriteAsync()` without blocking the calling thread.
   
---

### 3. Deserialization

Reconstructing objects from raw JSON text follows a four-step process:

1. **Reading the JSON Stream in Chunks:**  
   `ReadOneObjectJson()` reads the file asynchronously in 4KB buffers rather than loading everything at once. It tracks quotation marks and brace depth (`{` and `}`) to yield one complete JSON object string at a time, which is then passed to `ConvertJsonToObject()` for object reconstruction.

2. **Splitting into Key-Value Pairs:**  
   `ConvertJsonToObject()` passes the JSON string to `ParseJsonContent()`, which removes outer braces and splits the object text into individual property pairs using `GetPairs()`, ignoring commas placed inside strings or nested structures.

3. **Instantiating the Target Object:**  
   With the parsed pairs prepared, `ConvertJsonToObject()` inspects the target type using `GetValidConstructor()` to select the appropriate constructor (favoring `[JsonConstructor]`, then parameterless, then parameterized). `InvokeInitialObject()` then creates a default instance ready to be populated.

4. **Mapping Keys and Values to the Instantiated Object:**  
   `MapParsedValuesToObject()` iterates through the members retrieved via `ReflectionHelper.GetMembers()` and matches each member to its corresponding JSON key, checking for any custom names specified by `[JsonPropertyName]`. It then assigns the value based on the member type:
   - **Standard Values:** If the member is a primitive, string, or non-nested supported value, the text value is converted to the target data type and assigned directly.
   - **User-Defined Types:** If a property is a custom class (and not a string), `ConvertJsonToObject()` is called recursively to reconstruct the nested child object before assigning it to the parent property.
