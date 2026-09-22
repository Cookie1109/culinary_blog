import sys

file_path = 'backend/src/CulinaryBlog.Infrastructure/Content/ContentService.Composition.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

methods_to_wrap = [
    'public async Task<ChildMutationDto<IngredientDto>> CreateIngredientAsync',
    'public async Task<ChildMutationDto<IngredientDto>> UpdateIngredientAsync',
    'public async Task<long> DeleteIngredientAsync',
    'public async Task<ChildMutationDto<StepDto>> CreateStepAsync',
    'public async Task<ChildMutationDto<StepDto>> UpdateStepAsync',
    'public async Task<long> DeleteStepAsync'
]

catch_blocks = """
        catch (ContentProblemException)
        {
            throw;
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
        catch (Exception exception) when (IsUniqueViolation(exception, "RecipeSteps") || IsUniqueViolation(exception, "StepNumber"))
        {
            throw Conflict("STEP_NUMBER_CONFLICT", "A step number conflict occurred.");
        }
        catch (Exception exception) when (IsDeadlockOrSerialization(exception))
        {
            throw new ContentProblemException(
                "RECIPE_CONCURRENCY_CONFLICT", "The recipe was changed by another request.",
                ContentProblemKind.Conflict);
        }
"""

def wrap_method(method_decl, text):
    start_idx = text.find(method_decl)
    if start_idx == -1: return text
    
    brace_idx = text.find('{', start_idx)
    if brace_idx == -1: return text
    
    count = 1
    end_idx = brace_idx + 1
    while end_idx < len(text) and count > 0:
        if text[end_idx] == '{': count += 1
        elif text[end_idx] == '}': count -= 1
        end_idx += 1
        
    if count != 0: return text
    
    body = text[brace_idx+1:end_idx-1]
    
    indented_body = ''
    for line in body.split('\n'):
        if line.strip() == '':
            indented_body += '\n'
        else:
            indented_body += '    ' + line + '\n'
            
    if indented_body.endswith('\n'):
        indented_body = indented_body[:-1]
    
    new_body = '\n        try\n        {' + indented_body + '        }' + catch_blocks + '    '
    
    return text[:brace_idx+1] + new_body + text[end_idx-1:]

for method in methods_to_wrap:
    content = wrap_method(method, content)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Modified file successfully')
