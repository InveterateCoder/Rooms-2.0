import React, { Component } from "react";
import { Context } from "./data/Context";
import { Post } from "./utils/requests";
import Loading from "react-loading";
import urls from "./utils/Urls";

export class EConfirm extends Component {
    static contextType = Context;
    render() {
        return <div className="cover">
            <Loading id="spinner" type="spinningBubbles" color="#17a2b8" width="100px" />
        </div>
    }
    componentDidMount() {
        Post(urls.confirmEmail, this.props.match.params.number, this.context.lang)
            .then(data => {
                if (data) {
                    this.context.userRegistered(data);
                    this.props.history.replace("/lobby/1");
                } else this.props.history.replace("/signin/user/register");
            }).catch(() => this.props.history.push("/fatal"));
    }
}