# Squidex.Text

Helper methods to deal with text.

## Word Count

Calculate the number of words in a given text.

This method is inspired from the TinyMCE Plugin: https://github.com/tinymce/tinymce/tree/master/modules/polaris/src/main/ts/ephox/polaris/words

It works for the following scenarios:

* Support for the combination of CJK (Chinese, Japanese, Korean) letters and "normal" letters.
* Support for kanji characters.
* Allows punctuations in numer sequences (e.g. 3.15)
* Allows punations in words, e.g. `can't`.

### How to use it?

```
var wordCount = Words.Count("You can't do that, Mister."); // Returns 5
```

Also have a look to the unit test for more examples.

## Slugify

A slug is a piece of text in a URL with a meaning, e.g. the title of a blog post.

You can use the helper methods to calculate the slug automatically.


### How to use it?

```
var slug = "Hello World".Slugify(); // Returns "hello-world"
```

It also replaces some diacritics and special characters, e. the german "ä".


```
var slug = "Märchen".Slugify(); // Returns "maerchen"
```