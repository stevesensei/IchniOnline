using IchniOnline.Server.Mapper;
using IchniOnline.Server.Models;
using IchniOnline.Server.Models.Dto;
using IchniOnline.Server.Models.Game;
using System.Text.Json;

namespace IchniOnline.Test;

public class BeatmapJsonTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Test]
    public void Deserialize_BeatmapRoot_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
            "Beatmap": {
                "__type": "Ichni.RhythmGame.Beatmap.BeatmapContainer_BM,Assembly-CSharp",
                "value": {
                    "elementList": [
                        {
                            "__type": "Ichni.RhythmGame.Beatmap.Stay_BM,Assembly-CSharp",
                            "exactJudgeTime": 16.17,
                            "elementName": "Stay (16.17)",
                            "tags": [],
                            "elementGuid": {
                                "value": "c0a7832d-9d10-4d32-abf6-ca17409aab69"
                            },
                            "attachedElementGuid": {
                                "value": "63ac2902-f22d-4461-9f5c-73b57974d301"
                            }
                        },
                        {
                            "__type": "Ichni.RhythmGame.Beatmap.Tap_BM,Assembly-CSharp",
                            "exactJudgeTime": 49.7200623,
                            "elementName": "Tap (49.72006)",
                            "tags": [],
                            "elementGuid": {
                                "value": "b78202fe-d502-482f-9d61-268585b7ce9b"
                            },
                            "attachedElementGuid": {
                                "value": "085d1554-5d86-4afd-b415-acd4d70b5bfb"
                            }
                        }
                    ]
                }
            }
        }
        """;

        // Act
        var beatmapRoot = JsonSerializer.Deserialize<BeatmapRoot>(json, JsonOptions);

        // Assert
        Assert.That(beatmapRoot, Is.Not.Null);
        Assert.That(beatmapRoot.Beatmap, Is.Not.Null);
        Assert.That(beatmapRoot.Beatmap.SaveDataType, Is.EqualTo(SaveDataType.BeatmapContainer));
        Assert.That(beatmapRoot.Beatmap.Value, Is.Not.Null);
        Assert.That(beatmapRoot.Beatmap.Value.Elements, Is.Not.Null);
        Assert.That(beatmapRoot.Beatmap.Value.Elements.Count, Is.EqualTo(2));
    }

    [Test]
    public void Deserialize_StayElement_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
            "__type": "Ichni.RhythmGame.Beatmap.Stay_BM,Assembly-CSharp",
            "exactJudgeTime": 16.17,
            "elementName": "Stay (16.17)",
            "tags": [],
            "elementGuid": {
                "value": "c0a7832d-9d10-4d32-abf6-ca17409aab69"
            },
            "attachedElementGuid": {
                "value": "63ac2902-f22d-4461-9f5c-73b57974d301"
            }
        }
        """;

        // Act
        var element = JsonSerializer.Deserialize<StayElement>(json, JsonOptions);

        // Assert
        Assert.That(element, Is.Not.Null);
        Assert.That(element.SaveDataType, Is.EqualTo(SaveDataType.Stay));
        Assert.That(element.ExactJudgeTime, Is.EqualTo(16.17));
        Assert.That(element.ElementName, Is.EqualTo("Stay (16.17)"));
        Assert.That(element.ElementGuid.Value, Is.EqualTo("c0a7832d-9d10-4d32-abf6-ca17409aab69"));
        Assert.That(element.AttachedElementGuid.Value, Is.EqualTo("63ac2902-f22d-4461-9f5c-73b57974d301"));
        Assert.That(element.Tags, Is.Empty);
    }

    [Test]
    public void Deserialize_TapElement_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
            "__type": "Ichni.RhythmGame.Beatmap.Tap_BM,Assembly-CSharp",
            "exactJudgeTime": 49.7200623,
            "elementName": "Tap (49.72006)",
            "tags": [],
            "elementGuid": {
                "value": "b78202fe-d502-482f-9d61-268585b7ce9b"
            },
            "attachedElementGuid": {
                "value": "085d1554-5d86-4afd-b415-acd4d70b5bfb"
            }
        }
        """;

        // Act
        var element = JsonSerializer.Deserialize<TapElement>(json, JsonOptions);

        // Assert
        Assert.That(element, Is.Not.Null);
        Assert.That(element.SaveDataType, Is.EqualTo(SaveDataType.Tap));
        Assert.That(element.ExactJudgeTime, Is.EqualTo(49.7200623));
        Assert.That(element.ElementName, Is.EqualTo("Tap (49.72006)"));
    }

    [Test]
    public void Deserialize_ElementList_SkipsUnknownTypes()
    {
        // Arrange
        var json = """
        {
            "elementList": [
                {
                    "__type": "Ichni.RhythmGame.Beatmap.Stay_BM,Assembly-CSharp",
                    "exactJudgeTime": 16.17,
                    "elementName": "Stay (16.17)",
                    "tags": [],
                    "elementGuid": {
                        "value": "c0a7832d-9d10-4d32-abf6-ca17409aab69"
                    },
                    "attachedElementGuid": {
                        "value": "63ac2902-f22d-4461-9f5c-73b57974d301"
                    }
                },
                {
                    "__type": "Ichni.RhythmGame.Beatmap.ColorSubmodule_BM,Assembly-CSharp",
                    "originalBaseColor": {
                        "r": 1,
                        "g": 1,
                        "b": 1,
                        "a": 1
                    },
                    "emissionEnabled": false,
                    "attachedElementGuid": {
                        "value": "322fe95e-65df-42bc-9b22-1aff8c4656f2"
                    }
                },
                {
                    "__type": "Ichni.RhythmGame.Beatmap.Tap_BM,Assembly-CSharp",
                    "exactJudgeTime": 49.7200623,
                    "elementName": "Tap (49.72006)",
                    "tags": [],
                    "elementGuid": {
                        "value": "b78202fe-d502-482f-9d61-268585b7ce9b"
                    },
                    "attachedElementGuid": {
                        "value": "085d1554-5d86-4afd-b415-acd4d70b5bfb"
                    }
                }
            ]
        }
        """;

        // Act
        var wrapper = JsonSerializer.Deserialize<BeatmapWrapper>(json, JsonOptions);

        // Assert
        Assert.That(wrapper, Is.Not.Null);
        Assert.That(wrapper.Elements.Count, Is.EqualTo(2));
        Assert.That(wrapper.Elements[0], Is.InstanceOf<StayElement>());
        Assert.That(wrapper.Elements[1], Is.InstanceOf<TapElement>());
    }

    [Test]
    public void Deserialize_FullBeatmapJson_ParsesCorrectly()
    {
        // Arrange
        var json = """
        {
            "Beatmap": {
                "__type": "Ichni.RhythmGame.Beatmap.BeatmapContainer_BM,Assembly-CSharp",
                "value": {
                    "elementList": [
                        {
                            "__type": "Ichni.RhythmGame.Beatmap.Stay_BM,Assembly-CSharp",
                            "exactJudgeTime": 16.17,
                            "elementName": "Stay (16.17)",
                            "tags": [],
                            "elementGuid": {
                                "value": "c0a7832d-9d10-4d32-abf6-ca17409aab69"
                            },
                            "attachedElementGuid": {
                                "value": "63ac2902-f22d-4461-9f5c-73b57974d301"
                            }
                        },
                        {
                            "__type": "Ichni.RhythmGame.Beatmap.ColorSubmodule_BM,Assembly-CSharp",
                            "originalBaseColor": {
                                "r": 1,
                                "g": 1,
                                "b": 1,
                                "a": 1
                            },
                            "emissionEnabled": false,
                            "originalEmissionColor": {
                                "r": 0,
                                "g": 0,
                                "b": 0,
                                "a": 1
                            },
                            "originalEmissionIntensity": 0,
                            "attachedElementGuid": {
                                "value": "322fe95e-65df-42bc-9b22-1aff8c4656f2"
                            }
                        },
                        {
                            "__type": "Ichni.RhythmGame.Beatmap.Tap_BM,Assembly-CSharp",
                            "exactJudgeTime": 49.7200623,
                            "elementName": "Tap (49.72006)",
                            "tags": [],
                            "elementGuid": {
                                "value": "b78202fe-d502-482f-9d61-268585b7ce9b"
                            },
                            "attachedElementGuid": {
                                "value": "085d1554-5d86-4afd-b415-acd4d70b5bfb"
                            }
                        }
                    ]
                }
            }
        }
        """;

        // Act
        var beatmapRoot = JsonSerializer.Deserialize<BeatmapRoot>(json, JsonOptions);

        // Assert
        Assert.That(beatmapRoot, Is.Not.Null);
        Assert.That(beatmapRoot.Beatmap.Value.Elements.Count, Is.EqualTo(2));
        Assert.That(beatmapRoot.Beatmap.Value.Elements[0], Is.InstanceOf<StayElement>());
        Assert.That(beatmapRoot.Beatmap.Value.Elements[1], Is.InstanceOf<TapElement>());
    }

    [Test]
    public void ToNoteDtos_ExtractsOnlyNoteTypes()
    {
        // Arrange
        var json = """
        {
            "Beatmap": {
                "__type": "Ichni.RhythmGame.Beatmap.BeatmapContainer_BM,Assembly-CSharp",
                "value": {
                    "elementList": [
                        {
                            "__type": "Ichni.RhythmGame.Beatmap.Stay_BM,Assembly-CSharp",
                            "exactJudgeTime": 16.17,
                            "elementName": "Stay (16.17)",
                            "tags": [],
                            "elementGuid": {
                                "value": "c0a7832d-9d10-4d32-abf6-ca17409aab69"
                            }
                        },
                        {
                            "__type": "Ichni.RhythmGame.Beatmap.Tap_BM,Assembly-CSharp",
                            "exactJudgeTime": 49.7200623,
                            "elementName": "Tap (49.72006)",
                            "tags": [],
                            "elementGuid": {
                                "value": "b78202fe-d502-482f-9d61-268585b7ce9b"
                            }
                        },
                        {
                            "__type": "Ichni.RhythmGame.Beatmap.ColorSubmodule_BM,Assembly-CSharp",
                            "originalBaseColor": { "r": 1, "g": 1, "b": 1, "a": 1 },
                            "emissionEnabled": false,
                            "attachedElementGuid": {
                                "value": "322fe95e-65df-42bc-9b22-1aff8c4656f2"
                            }
                        }
                    ]
                }
            }
        }
        """;
        var beatmapRoot = JsonSerializer.Deserialize<BeatmapRoot>(json, JsonOptions);

        // Act
        var notes = BeatmapMapper.ToNoteDtos(beatmapRoot!);

        // Assert
        Assert.That(notes, Has.Count.EqualTo(2));
        Assert.That(notes[0].NoteGuid, Is.EqualTo(Guid.Parse("c0a7832d-9d10-4d32-abf6-ca17409aab69")));
        Assert.That(notes[0].NoteType, Is.EqualTo(SaveDataType.Stay));
        Assert.That(notes[0].JudgeTime, Is.EqualTo(16.17));
        Assert.That(notes[1].NoteGuid, Is.EqualTo(Guid.Parse("b78202fe-d502-482f-9d61-268585b7ce9b")));
        Assert.That(notes[1].NoteType, Is.EqualTo(SaveDataType.Tap));
        Assert.That(notes[1].JudgeTime, Is.EqualTo(49.7200623));
    }

    [Test]
    public void ToNoteDtos_ReturnsEmptyList_WhenNoNotes()
    {
        // Arrange
        var json = """
        {
            "Beatmap": {
                "__type": "Ichni.RhythmGame.Beatmap.BeatmapContainer_BM,Assembly-CSharp",
                "value": {
                    "elementList": [
                        {
                            "__type": "Ichni.RhythmGame.Beatmap.ColorSubmodule_BM,Assembly-CSharp",
                            "originalBaseColor": { "r": 1, "g": 1, "b": 1, "a": 1 },
                            "emissionEnabled": false,
                            "elementGuid": {
                                "value": "322fe95e-65df-42bc-9b22-1aff8c4656f2"
                            }
                        }
                    ]
                }
            }
        }
        """;
        var beatmapRoot = JsonSerializer.Deserialize<BeatmapRoot>(json, JsonOptions);

        // Act
        var notes = BeatmapMapper.ToNoteDtos(beatmapRoot!);

        // Assert
        Assert.That(notes, Is.Empty);
    }

    [Test]
    public void ToNoteDtos_ThrowsArgumentNullException_WhenRootIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BeatmapMapper.ToNoteDtos(null!));
    }
}
